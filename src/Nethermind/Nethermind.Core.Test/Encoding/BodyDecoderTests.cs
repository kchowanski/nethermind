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

using System.IO;
using Nethermind.Core.Crypto;
using Nethermind.Core.Encoding;
using Nethermind.Core.Extensions;
using Nethermind.Core.Specs;
using Nethermind.Core.Test.Builders;
using Nethermind.Logging;
using NUnit.Framework;

namespace Nethermind.Core.Test.Encoding
{
    [TestFixture]
    public class BodyDecoderTests
    {
        [Test]
        public void Can_do_roundtrip_null()
        {
            Rlp rlp = Rlp.Encode((BlockBody) null);
            BlockBody decoded = Rlp.Decode<BlockBody>(rlp);
            Assert.IsNull(decoded);
        }
        
        [Test]
        public void Can_do_roundtrip_null_memory_stream()
        {
            using (MemoryStream stream = Rlp.BorrowStream())
            {
                Rlp.Encode(stream,(BlockBody) null);
                BlockBody decoded = Rlp.Decode<BlockBody>(stream.ToArray());
                Assert.IsNull(decoded);
            }
        }
        
        [Test]
        public void Can_do_roundtrip_non_null_memory_stream()
        {
            Transaction tx = Build.A.Transaction.WithData(Bytes.FromHexString("0xaaaa")).Signed(new EthereumEcdsa(MainNetSpecProvider.Instance, LimboLogs.Instance), TestItem.PrivateKeyA, 0).TestObject;
            BlockHeader ommer = Build.A.BlockHeader.TestObject;
            
            BlockBody body = new BlockBody(new []{tx}, new[] {ommer});
            
            using (MemoryStream stream = Rlp.BorrowStream())
            {
                Rlp.Encode(stream, body);
                BlockBody decoded = Rlp.Decode<BlockBody>(stream.ToArray());
                Assert.AreEqual(body.Transactions[0].Hash, decoded.Transactions[0].Hash);
                Assert.AreEqual(body.Ommers[0].Hash, decoded.Ommers[0].Hash);
            }
        }
        
        [Test]
        public void Get_length_null()
        {
            BodyDecoder decoder = new BodyDecoder();
            Assert.AreEqual(1, decoder.GetLength(null, RlpBehaviors.None));
        }
        
        [Test]
        public void Can_handle_nulls()
        {
            Rlp rlp = Rlp.Encode((Block)null);
            Block decoded = Rlp.Decode<Block>(rlp);
            Assert.Null(decoded);
        }
    }
}