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

using System;
using System.IO;
using Nethermind.Core.Encoding;
using Nethermind.DataMarketplace.Consumers.Domain;
using Nethermind.DataMarketplace.Core.Domain;
using Org.BouncyCastle.Asn1.Cmp;

namespace Nethermind.DataMarketplace.Consumers.Infrastructure.Rlp
{
    public class DepositDetailsDecoder : IRlpDecoder<DepositDetails>
    {
        public static void Init()
        {
            // here to register with RLP in static constructor
        }

        static DepositDetailsDecoder()
        {
            Nethermind.Core.Encoding.Rlp.Decoders[typeof(DepositDetails)] = new DepositDetailsDecoder();
        }

        public DepositDetails Decode(Nethermind.Core.Encoding.Rlp.DecoderContext context,
            RlpBehaviors rlpBehaviors = RlpBehaviors.None)
        {
            try
            {
                var sequenceLength = context.ReadSequenceLength();
                if (sequenceLength == 0)
                {
                    return null;
                }

                var deposit = Nethermind.Core.Encoding.Rlp.Decode<Deposit>(context);
                var dataHeader = Nethermind.Core.Encoding.Rlp.Decode<DataHeader>(context);
                var pepper = context.DecodeByteArray();
                var timestamp = context.DecodeUInt();
                var transactionHash = context.DecodeKeccak();
                var verificationTimestamp = context.DecodeUInt();
                var earlyRefundTicket = Nethermind.Core.Encoding.Rlp.Decode<EarlyRefundTicket>(context);
                var claimedRefundTransactionHash = context.DecodeKeccak();
                var kyc = context.DecodeString();
                var confirmations = context.DecodeUInt();
                var requiredConfirmations = context.DecodeUInt();

                return new DepositDetails(deposit, dataHeader, pepper, timestamp, transactionHash,
                    verificationTimestamp, earlyRefundTicket, claimedRefundTransactionHash, kyc, confirmations,
                    requiredConfirmations);
            }
            catch (Exception)
            {
                context.Position = 0;
                var sequenceLength = context.ReadSequenceLength();
                if (sequenceLength == 0)
                {
                    return null;
                }

                var deposit = Nethermind.Core.Encoding.Rlp.Decode<Deposit>(context);
                var dataHeader = Nethermind.Core.Encoding.Rlp.Decode<DataHeader>(context);
                var pepper = context.DecodeByteArray();
                var transactionHash = context.DecodeKeccak();
                var verificationTimestamp = context.DecodeUInt();
                var earlyRefundTicket = Nethermind.Core.Encoding.Rlp.Decode<EarlyRefundTicket>(context);
                var claimedRefundTransactionHash = context.DecodeKeccak();
                var kyc = context.DecodeString();
                var confirmations = context.DecodeUInt();
                var requiredConfirmations = context.DecodeUInt();
                uint timestamp = 0;
                if (context.Position != context.Data.Length)
                {
                    timestamp = context.DecodeUInt();
                }

                return new DepositDetails(deposit, dataHeader, pepper, timestamp, transactionHash,
                    verificationTimestamp, earlyRefundTicket, claimedRefundTransactionHash, kyc, confirmations,
                    requiredConfirmations);
            }
        }

        public Nethermind.Core.Encoding.Rlp Encode(DepositDetails item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
        {
            if (item == null)
            {
                return Nethermind.Core.Encoding.Rlp.OfEmptySequence;
            }

            return Nethermind.Core.Encoding.Rlp.Encode(
                Nethermind.Core.Encoding.Rlp.Encode(item.Deposit),
                Nethermind.Core.Encoding.Rlp.Encode(item.DataHeader),
                Nethermind.Core.Encoding.Rlp.Encode(item.Pepper),
                Nethermind.Core.Encoding.Rlp.Encode(item.Timestamp),
                Nethermind.Core.Encoding.Rlp.Encode(item.TransactionHash),
                Nethermind.Core.Encoding.Rlp.Encode(item.VerificationTimestamp),
                Nethermind.Core.Encoding.Rlp.Encode(item.EarlyRefundTicket),
                Nethermind.Core.Encoding.Rlp.Encode(item.ClaimedRefundTransactionHash),
                Nethermind.Core.Encoding.Rlp.Encode(item.Kyc),
                Nethermind.Core.Encoding.Rlp.Encode(item.Confirmations),
                Nethermind.Core.Encoding.Rlp.Encode(item.RequiredConfirmations));
        }

        public void Encode(MemoryStream stream, DepositDetails item, RlpBehaviors rlpBehaviors = RlpBehaviors.None)
        {
            throw new System.NotImplementedException();
        }

        public int GetLength(DepositDetails item, RlpBehaviors rlpBehaviors)
        {
            throw new System.NotImplementedException();
        }
    }
}