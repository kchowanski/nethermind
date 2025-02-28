﻿/*
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

using System;
using System.Linq;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Encoding;
using Nethermind.Core.Extensions;
using Nethermind.DataMarketplace.Subprotocols.Messages;
using Nethermind.Network;

namespace Nethermind.DataMarketplace.Subprotocols.Serializers
{
    public class HiMessageSerializer : IMessageSerializer<HiMessage>
    {
        public byte[] Serialize(HiMessage message)
            => Rlp.Encode(Rlp.Encode(message.ProtocolVersion),
                Rlp.Encode(message.ProviderAddress),
                Rlp.Encode(message.ConsumerAddress),
                Rlp.Encode(message.NodeId.Bytes),
                Rlp.Encode(message.Signature.V),
                Rlp.Encode(message.Signature.R.WithoutLeadingZeros()),
                Rlp.Encode(message.Signature.S.WithoutLeadingZeros())).Bytes;

        public HiMessage Deserialize(byte[] bytes)
        {
            try
            {
                return Deserialize(bytes.AsRlpContext());
            }
            catch (Exception)
            {
                // strange garbage from p2p
                return Deserialize(bytes.Skip(3).ToArray().AsRlpContext());
            }
        }

        private static HiMessage Deserialize(Rlp.DecoderContext context)
        {
            context.ReadSequenceLength();
            var protocolVersion = context.DecodeByte();
            var providerAddress = context.DecodeAddress();
            var consumerAddress = context.DecodeAddress();
            var nodeId = new PublicKey(context.DecodeByteArray());
            var signature = SignatureDecoder.DecodeSignature(context);

            return new HiMessage(protocolVersion, providerAddress, consumerAddress,
                nodeId, signature);
        }
    }
}