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

using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Nethermind.WebSockets
{
    public class WebSocketsManager : IWebSocketsManager
    {
        private readonly ConcurrentDictionary<string, IWebSocketsModule> _modules =
            new ConcurrentDictionary<string, IWebSocketsModule>();

        public void AddModule(IWebSocketsModule module)
        {
            _modules.TryAdd(module.Name, module);

        }

        public IWebSocketsModule GetModule(string name)
            => _modules.TryGetValue(name, out var module) ? module : null;

        public IWebSocketsClient CreateClient(IWebSocketsModule module, WebSocket webSocket)
        {
            var client = module.CreateClient(webSocket);
            _modules.TryAdd(module.Name, module);

            return client;
        }
    }
}