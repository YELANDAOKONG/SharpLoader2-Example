# Sharp Loader (Example Module)

SharpLoader 示例模块（用于与“模组”区分）。

> 注意：当期项目硬编码了 SharpLoader.dll 和 SharpASM.dll 路径，请注意修改。

```csproj
<ItemGroup>
   <Reference Include="SharpLoader">
     <HintPath>..\..\SharpLoader\SharpLoader\bin\Release\net8.0\SharpLoader.dll</HintPath>
   </Reference>
        
   <Reference Include="SharpASM">
     <HintPath>..\..\SharpASM\SharpASM\bin\Release\net8.0\SharpASM.dll</HintPath>
   </Reference>
</ItemGroup>
```

## 效果

使用 SharpASM 修改了玩家实体的 `readCustomData` 方法，并在方法返回前修改 `this.experienceLevel` 为 `114514`。

![MINECRAFT](README.png)

> 插入的字节码：

```text
[@] - ALOAD_0 [] 
[@] - LDC_W [0x0C07] 
[@] - PUTFIELD [0x0C0A] 
[@] - RETURN [] 
```

```csharp
codes.RemoveAt(codes.Count - 1); // 删除原有 RETURN 
codes.Add(new Code(OperationCode.ALOAD_0)); // 插入字节码...
codes.Add(new Code(OperationCode.LDC_W, new[] { Operand.WideIndex(newIndex) }));
codes.Add(new Code(OperationCode.PUTFIELD, new[] { Operand.FieldRef(fieldRefIndex) }));
codes.Add(new Code(OperationCode.RETURN));
```

