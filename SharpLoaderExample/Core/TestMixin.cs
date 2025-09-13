using SharpASM.Models;
using SharpASM.Models.Code;
using SharpASM.Models.Struct.Attribute;
using SharpMixin.Attributes;
using SharpMixin.Models;

namespace SharpLoaderExample.Core;

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
}