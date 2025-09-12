using SharpASM.Helpers.Models.Type;
using SharpASM.Models;
using SharpASM.Models.Code;
using SharpASM.Models.Struct;
using SharpASM.Models.Struct.Attribute;
using SharpASM.Parsers;
using SharpLoader;
using SharpLoader.Core.Minecraft.Mapping.Models;
using SharpLoader.Core.Modding;
using SharpLoader.Modding;
using SharpLoader.Utilities;

namespace SharpLoaderExample;

public class ModuleMain : ModuleBase
{
    public static ModuleMain? Instance { get; private set; }

    public ModuleMain()
    {
        Instance = this;
    }

    protected override void OnInitialize(ModuleManager manager, LoggerService? logger)
    {
        Logger?.Info("Module loaded.");
    }
    

    public override byte[]? ModifyClass(string className, byte[]? classData)
    {
        if (Manager == null) return null;
        var classMappedName = Manager.Mapping.Classes.TryGetValue(className, out var mappedClass);
        var innerClassMappedName = Manager.Mapping.InnerClasses.TryGetValue(className, out var mappedInnerClass);
        if (classMappedName) Logger?.Trace($"(Mappings) {className} => {mappedClass?.MappedName}");
        if (innerClassMappedName) Logger?.Trace($"(Mappings Inner) {className} => {mappedInnerClass?.MappedName}");

        if (mappedClass?.MappedName == "net/minecraft/entity/player/PlayerEntity")
        {
            if (classData == null || classData.Length == 0) return [];
            Logger?.Trace($"(Class Data) 0x{BitConverter.ToString(classData).Replace("-", "")} [{classData.Length} Bytes]");
            
            ClassStruct cStruct = ClassParser.Parse(classData);
            Class clazz = Class.FromStruct(cStruct);
            var helper = clazz.GetConstantPoolHelper();
            string? fieldN = null;
            string? fieldD = null;
            
            Logger?.Warn($"(Class Struct) \n{clazz}");
            
            Logger?.Warn($"[@] This Class: {clazz.ThisClass}");
            Logger?.Warn($"[@] Super Class: {clazz.SuperClass}");
            foreach (var field in clazz.Fields)
            {
                Logger?.Warn($"[@] - Field: {field.Name} {field.Descriptor} [ {FieldAccessFlagsHelper.GetFlagsString((ushort)field.AccessFlags)} ]");
                foreach (var attribute in field.Attributes)
                {
                    Logger?.Warn($"[@] -  - Attribute: {attribute.Name} ({attribute.Info.Length} Bytes)");
                }
                foreach (var mappedClassField in mappedClass.Fields)
                {
                    if (mappedClassField.ObfuscatedName == field.Name)
                    {
                        if (mappedClassField.MappedName == "experienceLevel")
                        {
                            Logger?.Error("Found obfuscated field (Mapped Name)");
                            fieldN = field.Name;
                            fieldD = field.Descriptor;
                            foreach (var attribute in field.Attributes)
                            {
                                if (attribute.Name == "ConstantValue")
                                {
                                    var newIndex = helper.NewInteger(114514);
                                    ConstantValueAttributeStruct cValue = new ConstantValueAttributeStruct();
                                    cValue.ConstantValueIndex = newIndex;
                                    attribute.Info = cValue.ToBytesWithoutIndexAndLength();
                                }
                            }
                        }
                    }
                }
            }
            
            var superClass = Manager.Mapping.Classes.Select((e) =>
            {
                if (e.Value.MappedName == "net/minecraft/entity/Entity")
                {
                    return e.Value;
                }

                return null;
            }).ToList();
            ClassMapping super = superClass[0]!;
            
            foreach (var method in clazz.Methods)
            {
                
                Logger?.Warn($"[@] - Method: {method.Name} {method.Descriptor} [ {MethodAccessFlagsHelper.GetFlagsString((ushort)method.AccessFlags)} ]");
                foreach (var attribute in method.Attributes)
                {
                    Logger?.Warn($"[@] -  - Attribute: {attribute.Name} ({attribute.Info.Length} Bytes)");
                    if (attribute.Name == "Code")
                    {
                        AttributeInfoStruct attributeInfoStruct = new AttributeInfoStruct()
                        {
                            AttributeLength = 0,
                            AttributeNameIndex = 0,
                            Info = attribute.Info,
                        };
                        CodeAttributeStruct attributeStruct = CodeAttributeStruct.FromStructInfo(attributeInfoStruct);
                        var codes = ByteCodeParser.Parse(attributeStruct.Code);
                        foreach (var code in codes)
                        {
                            Logger?.Warn($"[@] -  -  - {code}");
                        }
                    }
                }

                var allMethods = mappedClass.Methods.ToList();
                allMethods.AddRange(super.Methods);
                foreach (var mappedMethod in allMethods)
                {
                    if (fieldN == null || fieldD == null)
                    {
                        break;
                    }
                    if (mappedMethod.ObfuscatedName == method.Name)
                    {
                        if (mappedMethod.MappedName == "readCustomData")
                        {
                            Logger?.Error("Found obfuscated method (Mapped Name)");
                            foreach (var attribute in method.Attributes)
                            {
                                if (attribute.Name == "Code")
                                {
                                    var newIndex = helper.NewInteger(114514);
                                    var fieldRefIndex = helper.NewFieldref(clazz.ThisClass, fieldN, fieldD);
                                    AttributeInfoStruct ciValue = new AttributeInfoStruct();
                                    ciValue.AttributeLength = 0; // Will be ignored
                                    ciValue.AttributeNameIndex = 0; // Will be ignored
                                    ciValue.Info = attribute.Info;
                                    CodeAttributeStruct cValue = CodeAttributeStruct.FromStructInfo(ciValue);
                                    var codes = cValue.GetCode();
                                    
                                    codes.RemoveAt(codes.Count - 1);
                                    codes.Add(new Code(OperationCode.ALOAD_0));
                                    codes.Add(new Code(OperationCode.LDC_W, new[] { Operand.WideIndex(newIndex) }));
                                    codes.Add(new Code(OperationCode.PUTFIELD, new[] { Operand.FieldRef(fieldRefIndex) }));
                                    codes.Add(new Code(OperationCode.RETURN));
                                    cValue.SetCode(codes);
                                    
                                    attribute.Info = cValue.ToBytesWithoutIndexAndLength();
                                    Logger?.Error("Successful to modify the method.");
                                    foreach (var code in codes)
                                    {
                                        Logger?.Error($"[@] -  -  - {code}");
                                    }
                                }
                            }
                        }
                    }
                }
            }

            clazz.ConstantPool = helper.ToList();
            var newClass = clazz.ToStruct();
            return ClassParser.Serialize(newClass);
        }
        return null;
    }
}