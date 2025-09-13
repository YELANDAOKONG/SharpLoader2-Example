using SharpASM.Models.Code;

namespace SharpLoaderExample.Core
{
    public static class BytecodeInserter
    {
        /// <summary>
        /// 在指定位置插入字节码并自动调整偏移量
        /// </summary>
        /// <param name="originalCodes">原始字节码列表</param>
        /// <param name="insertIndex">插入位置索引</param>
        /// <param name="codesToInsert">要插入的字节码列表</param>
        /// <returns>修改后的字节码列表</returns>
        public static List<Code> InsertCodes(List<Code> originalCodes, int insertIndex, List<Code> codesToInsert)
        {
            if (insertIndex < 0 || insertIndex > originalCodes.Count)
                throw new ArgumentOutOfRangeException(nameof(insertIndex), "Insert index is out of range");
            
            if (codesToInsert == null || codesToInsert.Count == 0)
                return originalCodes;
            
            // 计算插入代码的总长度
            int insertLength = CalculateCodeLength(codesToInsert);
            
            // 创建新代码列表
            var newCodes = new List<Code>(originalCodes);
            
            // 插入新代码
            newCodes.InsertRange(insertIndex, codesToInsert);
            
            // 调整所有跳转指令的偏移量
            AdjustBranchOffsets(newCodes, insertIndex, insertLength, true);
            
            return newCodes;
        }
        
        /// <summary>
        /// 删除指定位置的字节码并自动调整偏移量
        /// </summary>
        /// <param name="originalCodes">原始字节码列表</param>
        /// <param name="removeIndex">删除起始位置索引</param>
        /// <param name="removeCount">要删除的指令数量</param>
        /// <returns>修改后的字节码列表</returns>
        public static List<Code> RemoveCodes(List<Code> originalCodes, int removeIndex, int removeCount)
        {
            if (removeIndex < 0 || removeIndex >= originalCodes.Count)
                throw new ArgumentOutOfRangeException(nameof(removeIndex), "Remove index is out of range");
            
            if (removeCount <= 0 || removeIndex + removeCount > originalCodes.Count)
                throw new ArgumentOutOfRangeException(nameof(removeCount), "Remove count is invalid");
            
            // 计算删除代码的总长度
            int removeLength = CalculateCodeLength(originalCodes.GetRange(removeIndex, removeCount));
            
            // 创建新代码列表
            var newCodes = new List<Code>(originalCodes);
            
            // 删除代码
            newCodes.RemoveRange(removeIndex, removeCount);
            
            // 调整所有跳转指令的偏移量
            AdjustBranchOffsets(newCodes, removeIndex, -removeLength, false);
            
            return newCodes;
        }
        
        /// <summary>
        /// 计算字节码列表的总长度
        /// </summary>
        private static int CalculateCodeLength(List<Code> codes)
        {
            int length = 0;
            
            foreach (var code in codes)
            {
                // 前缀长度
                if (code.Prefix.HasValue)
                    length += 1;
                
                // 操作码长度
                length += 1;
                
                // 操作数长度
                if (OperationCodeMapping.TryGetOperandInfo(code.OpCode, out int operandCount, out int[] operandSizes))
                {
                    for (int i = 0; i < Math.Min(operandCount, code.Operands.Count); i++)
                    {
                        length += operandSizes[i];
                    }
                }
            }
            
            return length;
        }
        
        /// <summary>
        /// 调整所有跳转指令的偏移量
        /// </summary>
        private static void AdjustBranchOffsets(List<Code> codes, int changeIndex, int delta, bool isInsertion)
        {
            // 计算每个指令的偏移量
            var offsets = CalculateInstructionOffsets(codes);
            
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];
                
                // 检查是否为跳转指令
                if (IsBranchInstruction(code.OpCode))
                {
                    // 获取跳转偏移量操作数
                    if (code.Operands.Count > 0)
                    {
                        var operand = code.Operands[0];
                        short offset = BitConverter.ToInt16(operand.Data);
                        
                        // 计算跳转目标索引
                        int targetOffset = offsets[i] + offset;
                        int targetIndex = FindInstructionIndexByOffset(offsets, targetOffset);
                        
                        // 根据插入/删除操作调整偏移量
                        if (isInsertion)
                        {
                            // 插入操作：如果插入点在当前指令之后且跳转目标在插入点之前，偏移量不变
                            // 如果插入点在跳转目标之前，需要增加偏移量
                            if (changeIndex <= targetIndex)
                            {
                                offset += (short)delta;
                                code.Operands[0] = Operand.BranchOffset(offset);
                            }
                        }
                        else
                        {
                            // 删除操作：如果删除点在当前指令之后且跳转目标在删除点之前，偏移量不变
                            // 如果删除点在跳转目标之前，需要减少偏移量
                            if (changeIndex <= targetIndex)
                            {
                                offset -= (short)delta;
                                code.Operands[0] = Operand.BranchOffset(offset);
                            }
                        }
                    }
                }
                // 处理宽跳转指令（GOTO_W, JSR_W）
                else if (code.OpCode == OperationCode.GOTO_W || code.OpCode == OperationCode.JSR_W)
                {
                    if (code.Operands.Count > 0)
                    {
                        var operand = code.Operands[0];
                        int offset = BitConverter.ToInt32(operand.Data);
                        
                        // 计算跳转目标索引
                        int targetOffset = offsets[i] + offset;
                        int targetIndex = FindInstructionIndexByOffset(offsets, targetOffset);
                        
                        // 根据插入/删除操作调整偏移量
                        if (isInsertion)
                        {
                            if (changeIndex <= targetIndex)
                            {
                                offset += delta;
                                code.Operands[0] = Operand.WideBranchOffset(offset);
                            }
                        }
                        else
                        {
                            if (changeIndex <= targetIndex)
                            {
                                offset -= delta;
                                code.Operands[0] = Operand.WideBranchOffset(offset);
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 计算每个指令的偏移量
        /// </summary>
        private static int[] CalculateInstructionOffsets(List<Code> codes)
        {
            int[] offsets = new int[codes.Count];
            int currentOffset = 0;
            
            for (int i = 0; i < codes.Count; i++)
            {
                offsets[i] = currentOffset;
                currentOffset += CalculateCodeLength(new List<Code> { codes[i] });
            }
            
            return offsets;
        }
        
        /// <summary>
        /// 根据偏移量查找指令索引
        /// </summary>
        private static int FindInstructionIndexByOffset(int[] offsets, int targetOffset)
        {
            for (int i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] == targetOffset)
                    return i;
                
                if (i < offsets.Length - 1 && offsets[i] < targetOffset && offsets[i + 1] > targetOffset)
                    return i;
            }
            
            return -1;
        }
        
        /// <summary>
        /// 检查是否为跳转指令
        /// </summary>
        private static bool IsBranchInstruction(OperationCode opCode)
        {
            return opCode == OperationCode.IFEQ ||
                   opCode == OperationCode.IFNE ||
                   opCode == OperationCode.IFLT ||
                   opCode == OperationCode.IFGE ||
                   opCode == OperationCode.IFGT ||
                   opCode == OperationCode.IFLE ||
                   opCode == OperationCode.IF_ICMPEQ ||
                   opCode == OperationCode.IF_ICMPNE ||
                   opCode == OperationCode.IF_ICMPLT ||
                   opCode == OperationCode.IF_ICMPGE ||
                   opCode == OperationCode.IF_ICMPGT ||
                   opCode == OperationCode.IF_ICMPLE ||
                   opCode == OperationCode.IF_ACMPEQ ||
                   opCode == OperationCode.IF_ACMPNE ||
                   opCode == OperationCode.IFNULL ||
                   opCode == OperationCode.IFNONNULL ||
                   opCode == OperationCode.GOTO ||
                   opCode == OperationCode.JSR;
        }
    }
}
