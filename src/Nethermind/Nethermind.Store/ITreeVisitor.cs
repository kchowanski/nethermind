/*
 * Copyright (c) 2018 Demerzel Solutions Limited
 * This file is part of the Nethermind library.
 *
 * The Nethermind library is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * The Nethermind library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
 */

using Nethermind.Core.Crypto;

namespace Nethermind.Store
{
    public class VisitContext
    {
        public int Level { get; set; }

        public bool IsStorage { get; set; }

        public int? BranchChildIndex { get; set; }
    }
    
    public interface ITreeVisitor
    {
        void VisitTree(Keccak rootHash, VisitContext context);
        
        void VisitMissingNode(Keccak nodeHash, VisitContext context);
        
        void VisitBranch(byte[] hashOrRlp, VisitContext context);
        
        void VisitExtension(byte[] hashOrRlp, VisitContext context);
        
        void VisitLeaf(byte[] hashOrRlp, VisitContext context);
        
        void VisitCode(Keccak codeHash, byte[] code, VisitContext context);
    }
}