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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Encoding;
using Nethermind.Core.Extensions;
using Nethermind.DataMarketplace.Consumers.Domain;
using Nethermind.DataMarketplace.Consumers.Queries;
using Nethermind.DataMarketplace.Consumers.Repositories;
using Nethermind.DataMarketplace.Core;
using Nethermind.DataMarketplace.Core.Domain;
using Nethermind.Store;

namespace Nethermind.DataMarketplace.Consumers.Infrastructure.Persistence.Rocks.Repositories
{
    public class ConsumerSessionRocksRepository : IConsumerSessionRepository
    {
        private readonly IDb _database;
        private readonly IRlpDecoder<ConsumerSession> _rlpDecoder;

        public ConsumerSessionRocksRepository(IDb database, IRlpDecoder<ConsumerSession> rlpDecoder)
        {
            _database = database;
            _rlpDecoder = rlpDecoder;
        }

        public Task<ConsumerSession> GetAsync(Keccak id) => Task.FromResult(Decode(_database.Get(id)));

        public Task<ConsumerSession> GetPreviousAsync(ConsumerSession session)
        {
            var sessions = Filter(session.DepositId);
            switch (sessions.Count)
            {
                case 0:
                    return Task.FromResult<ConsumerSession>(null);
                case 1:
                    return Task.FromResult(GetUniqueSession(session, sessions[0]));
                default:
                {
                    var previousSessions = sessions.Take(2).ToArray();

                    return Task.FromResult(GetUniqueSession(session, previousSessions[1]) ??
                                           GetUniqueSession(session, previousSessions[0]));
                }
            }
        }

        private static ConsumerSession GetUniqueSession(ConsumerSession current, ConsumerSession previous)
            => current.Equals(previous) ? null : previous;


        public Task<PagedResult<ConsumerSession>> BrowseAsync(GetConsumerSessions query)
            => Task.FromResult(Filter(query.DepositId, query.DataHeaderId, query.ConsumerNodeId, query.ConsumerAddress,
                query.ProviderNodeId, query.ProviderAddress).Paginate(query));

        private IReadOnlyList<ConsumerSession> Filter(Keccak depositId = null, Keccak dataHeaderId = null,
            PublicKey consumerNodeId = null, Address consumerAddress = null, PublicKey providerNodeId = null,
            Address providerAddress = null)
        {
            var sessionsBytes = _database.GetAll();
            if (sessionsBytes.Length == 0)
            {
                return Array.Empty<ConsumerSession>();
            }

            var sessions = new ConsumerSession[sessionsBytes.Length];
            for (var i = 0; i < sessionsBytes.Length; i++)
            {
                sessions[i] = Decode(sessionsBytes[i]);
            }

            if (depositId is null && dataHeaderId is null && consumerNodeId is null && consumerAddress is null
                && providerNodeId is null && providerAddress is null)
            {
                return sessions;
            }

            var filteredSessions = sessions.AsEnumerable();
            if (!(depositId is null))
            {
                filteredSessions = filteredSessions.Where(s => s.DepositId == depositId);
            }

            if (!(dataHeaderId is null))
            {
                filteredSessions = filteredSessions.Where(s => s.DataHeaderId == dataHeaderId);
            }

            if (!(consumerNodeId is null))
            {
                filteredSessions = filteredSessions.Where(s => s.ConsumerNodeId == consumerNodeId);
            }

            if (!(consumerAddress is null))
            {
                filteredSessions = filteredSessions.Where(s => s.ConsumerAddress == consumerAddress);
            }

            if (!(providerNodeId is null))
            {
                filteredSessions = filteredSessions.Where(s => s.ProviderNodeId == providerNodeId);
            }

            if (!(providerAddress is null))
            {
                filteredSessions = filteredSessions.Where(s => s.ProviderAddress == providerAddress);
            }

            return filteredSessions.OrderByDescending(s => s.StartTimestamp).ToArray();
        }

        public Task AddAsync(ConsumerSession session) => AddOrUpdateAsync(session);

        public Task UpdateAsync(ConsumerSession session) => AddOrUpdateAsync(session);

        private Task AddOrUpdateAsync(ConsumerSession session)
        {
            var rlp = _rlpDecoder.Encode(session);
            _database.Set(session.Id, rlp.Bytes);

            return Task.CompletedTask;
        }

        private ConsumerSession Decode(byte[] bytes)
            => bytes is null
                ? null
                : _rlpDecoder.Decode(bytes.AsRlpContext());
    }
}