using SharpASM.Analysis.Executor;
using SharpASM.Models;
using SharpASM.Models.Code;
using SharpASM.Models.Struct;
using SharpASM.Models.Struct.Attribute;
using SharpASM.Models.Type;
using SharpASM.Parsers;
using SharpASM.Utilities;
using SharpMixin.Attributes;
using SharpMixin.Models;
using Attribute = SharpASM.Models.Attribute;

namespace SharpLoaderExample.Core;

[Mixin]
public class TestMixin
{
    [MethodCodeMixin("net/minecraft/class_1657", 
        "method_5749", 
        "(Lnet/minecraft/class_11368;)V", 
        NameType.Default)]
    public static CodeAttributeStruct TestMixinMethod(Class clazz, CodeAttributeStruct attribute)
    {
        List<Code> codes = attribute.GetCode();
        var helper = clazz.GetConstantPoolHelper();
        var fieldName = "field_7520";
        var newIndex = helper.NewInteger(114514);
        var fieldRefIndex = helper.NewFieldref(clazz.ThisClass, fieldName, "I");
        
        codes.RemoveAt(codes.Count - 1);
        codes.Add(new Code(OperationCode.ALOAD_0));
        codes.Add(new Code(OperationCode.LDC_W, new[] { Operand.WideIndex(newIndex) }));
        codes.Add(new Code(OperationCode.PUTFIELD, new[] { Operand.FieldRef(fieldRefIndex) }));
        codes.Add(new Code(OperationCode.RETURN));

        clazz.ConstantPool = helper.ToList();     
        attribute.SetCode(codes);
        return attribute;
    }

    // [MethodCodeMixin("net/minecraft/class_1657",
    //     "method_7324",
    //     "(Lnet/minecraft/class_1297;)V",
    //     NameType.Default)]
    // public static CodeAttributeStruct TestPlusMixinMethod(Class clazz, CodeAttributeStruct attribute)
    // {
    //     List<Code> codes = attribute.GetCode();
    //     var helper = clazz.GetConstantPoolHelper();
    //
    //
    //     ByteCodeInserter.InsertCodes(...);
    //     clazz.ConstantPool = helper.ToList();
    //     attribute.SetCode(codes);
    //     return attribute;
    // }
    
    [MethodCodeMixin("net/minecraft/class_1657", 
        "method_7324", 
        "(Lnet/minecraft/class_1297;)V", 
        NameType.Default)]
    public static CodeAttributeStruct TestPlusMixinMethod(Class clazz, Method method, CodeAttributeStruct attribute)
    {
        List<Code> codes = attribute.GetCode();
        var helper = clazz.GetConstantPoolHelper();
    
        // Find the index where damage calculation occurs
        int targetIndex = -1;
        for (int i = 0; i < codes.Count - 2; i++)
        {
            if (codes[i].OpCode == OperationCode.FMUL &&
                codes[i + 1].OpCode == OperationCode.FSTORE_2 &&
                codes[i + 2].OpCode == OperationCode.FLOAD &&
                codes[i + 2].Operands.Count > 0 &&
                codes[i + 2].Operands[0].Data[0] == 0x05)
            {
                targetIndex = i + 1; // Insert after FSTORE_2
                break;
            }
        }

        if (targetIndex != -1)
        {
            // Create 1000.0f constant
            var floatIndex = helper.NewFloat(1000.0f);
        
            // Create multiplication instructions
            List<Code> damageBoost = new List<Code>
            {
                new Code(OperationCode.FLOAD_2),
                new Code(OperationCode.LDC_W, new[] { Operand.WideIndex(floatIndex) }),
                new Code(OperationCode.FMUL),
                new Code(OperationCode.FSTORE_2)
            };

            // Insert the damage boost code
            codes = ByteCodeInserter.InsertCodes(codes, targetIndex + 1, damageBoost);
        }

        attribute.SetCode(codes);
        clazz.ConstantPool = helper.ToList();

        CodeExecutor executor = new CodeExecutor(clazz, method, attribute, attribute.GetCode());
        var map = executor.RebuildStackMapTable();
        var table = map.ToBytesWithoutIndexAndLength();
        helper = clazz.GetConstantPoolHelper();
        var appended = false;
        foreach (var attributeInfoStruct in attribute.Attributes)
        {
            appended = true;
            var data = helper.ByIndex(attributeInfoStruct.AttributeNameIndex);
            if (data == null)
            {
                continue;
            }

            if (data.Tag != ConstantPoolTag.Utf8)
            {
                continue;
            }

            int offset = 0;
            ConstantUtf8InfoStruct utf8 = ConstantUtf8InfoStruct.FromBytesWithTag((byte)ConstantPoolTag.Utf8, attributeInfoStruct.Info, ref offset);
            if (utf8.ToString() == "StackMapTable")
            {
                appended = true;
                attributeInfoStruct.Info = table;
                attributeInfoStruct.AttributeLength = (uint)table.Length;
            }
        }

        if (!appended)
        {
            var list = new List<AttributeInfoStruct>(attribute.Attributes);
            list.Add(new AttributeInfoStruct()
            {
                AttributeNameIndex = helper.NewUtf8("StackMapTable"),
                AttributeLength = (uint)table.Length,
                Info = table,
            });
            attribute.Attributes = list.ToArray();
        }
        clazz.ConstantPool = helper.ToList();
        return attribute;
    }





    [ClassMixin("net/minecraft/class_1657", NameType.Default)]
    public static Class TestMixinClass(Class clazz)
    {
        return clazz;
    }
}