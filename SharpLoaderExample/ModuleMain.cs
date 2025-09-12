using SharpASM.Helpers.Models.Type;
using SharpASM.Models;
using SharpASM.Models.Struct;
using SharpASM.Parsers;
using SharpLoader;
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
            
            Logger?.Warn($"(Class Struct) \n{clazz}");
            
            Logger?.Warn($"[@] This Class: {clazz.ThisClass}");
            Logger?.Warn($"[@] Super Class: {clazz.SuperClass}");
            foreach (var field in clazz.Fields)
            {
                Logger?.Warn($"[@] - Field: {field.Name} {field.Descriptor} [ {FieldAccessFlagsHelper.GetFlagsString((ushort)field.AccessFlags)} ]");
            }
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
            }

            return classData;
        }
        return null;
    }
}