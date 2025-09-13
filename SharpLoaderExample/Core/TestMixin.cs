using SharpASM.Models.Code;
using SharpMixin.Attributes;
using SharpMixin.Models;

namespace SharpLoaderExample.Core;

public class TestMixin
{
    [MethodMixin("net/minecraft/class_1657", 
        "method_5749", 
        "()V", 
        NameType.ObfuscatedName)]
    public static List<Code> TestMixinMethod(List<Code> codes)
    {
        return codes;
    }
}