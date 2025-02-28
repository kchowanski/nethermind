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

using System.Security;
using Nethermind.Abi;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Filters;
using Nethermind.Core;
using Nethermind.Core.Specs;
using Nethermind.DataMarketplace.Channels;
using Nethermind.DataMarketplace.Core.Services;
using Nethermind.DataMarketplace.Infrastructure.Rlp;
using Nethermind.Evm;
using Nethermind.Facade;
using Nethermind.Logging;
using Nethermind.Store;
using Nethermind.Wallet;

namespace Nethermind.DataMarketplace.Infrastructure.Modules
{
    public class NdmModule : INdmModule
    {
        public INdmServices Init(NdmRequiredServices services)
        {
            AddDecoders();
            var config = services.NdmConfig;
            var providerAddress = string.IsNullOrWhiteSpace(config.ProviderAddress)
                ? Address.Zero
                : new Address(config.ProviderAddress);
            var consumerAddress = string.IsNullOrWhiteSpace(config.ConsumerAddress)
                ? Address.Zero
                : new Address(config.ConsumerAddress);
            var contractAddress = string.IsNullOrWhiteSpace(config.ContractAddress)
                ? Address.Zero
                : new Address(config.ContractAddress);
            UnlockHardcodedAccounts(providerAddress, consumerAddress, services.Wallet);
            var readOnlyDbProvider = new ReadOnlyDbProvider(services.RocksProvider, false);
            var filterStore = new FilterStore();
            var filterManager = new FilterManager(filterStore, services.BlockProcessor, services.TransactionPool,
                services.LogManager);
            var state = new RpcState(services.BlockTree, services.SpecProvider, readOnlyDbProvider,
                services.LogManager);
            var blockchainBridge = new BlockchainBridge(
                state.StateReader,
                state.StateProvider,
                state.StorageProvider,
                state.BlockTree,
                services.TransactionPool,
                services.TransactionPoolInfoProvider,
                services.ReceiptStorage,
                filterStore,
                filterManager,
                services.Wallet,
                state.TransactionProcessor,
                services.Ecdsa);
            var dataHeaderRlpDecoder = new DataHeaderDecoder();
            var encoder = new AbiEncoder();
            var depositService = new DepositService(blockchainBridge, encoder, services.Wallet, contractAddress,
                LimboLogs.Instance);
            var ndmConsumerChannelManager = services.NdmConsumerChannelManager;
            var ndmDataPublisher = services.NdmDataPublisher;
            var jsonRpcNdmConsumerChannel = new JsonRpcNdmConsumerChannel();
//            ndmConsumerChannelManager.Add(jsonRpcNdmConsumerChannel);

            return new Services(services, new NdmCreatedServices(consumerAddress, encoder, dataHeaderRlpDecoder,
                depositService, ndmDataPublisher, jsonRpcNdmConsumerChannel, ndmConsumerChannelManager,
                blockchainBridge));
        }

        private static void AddDecoders()
        {
            DataDeliveryReceiptDecoder.Init();
            DataDeliveryReceiptRequestDecoder.Init();
            DataDeliveryReceiptToMergeDecoder.Init();
            DataDeliveryReceiptDetailsDecoder.Init();
            DataHeaderDecoder.Init();
            DataHeaderRuleDecoder.Init();
            DataHeaderRulesDecoder.Init();
            DataHeaderProviderDecoder.Init();
            DataRequestDecoder.Init();
            DepositDecoder.Init();
            DepositApprovalDecoder.Init();
            EarlyRefundTicketDecoder.Init();
            EthRequestDecoder.Init();
            FaucetResponseDecoder.Init();
            FaucetRequestDetailsDecoder.Init();
            SessionDecoder.Init();
            UnitsRangeDecoder.Init();
        }

        private static void UnlockHardcodedAccounts(Address providerAddress, Address consumerAddress, IWallet wallet)
        {
            // hardcoded passwords
            var consumerPassphrase = new SecureString();
            foreach (var c in "ndmConsumer")
            {
                consumerPassphrase.AppendChar(c);
            }

            consumerPassphrase.MakeReadOnly();
            wallet.UnlockAccount(consumerAddress, consumerPassphrase);
        }

        private class RpcState
        {
            public readonly IStateReader StateReader;
            public readonly IStateProvider StateProvider;
            public readonly IStorageProvider StorageProvider;
            public readonly IBlockhashProvider BlockhashProvider;
            public readonly IVirtualMachine VirtualMachine;
            public readonly TransactionProcessor TransactionProcessor;
            public readonly IBlockTree BlockTree;

            public RpcState(IBlockTree blockTree, ISpecProvider specProvider, IReadOnlyDbProvider readOnlyDbProvider,
                ILogManager logManager)
            {
                var stateDb = readOnlyDbProvider.StateDb;
                var codeDb = readOnlyDbProvider.CodeDb;
                StateReader = new StateReader(readOnlyDbProvider.StateDb, codeDb, logManager);
                StateProvider = new StateProvider(stateDb, codeDb, logManager);
                StorageProvider = new StorageProvider(stateDb, StateProvider, logManager);
                BlockTree = new ReadOnlyBlockTree(blockTree);
                BlockhashProvider = new BlockhashProvider(BlockTree, logManager);
                VirtualMachine = new VirtualMachine(StateProvider, StorageProvider, BlockhashProvider, specProvider,
                    logManager);
                TransactionProcessor = new TransactionProcessor(specProvider, StateProvider, StorageProvider,
                    VirtualMachine, logManager);
            }
        }

        private class Services : INdmServices
        {
            public NdmRequiredServices RequiredServices { get; }
            public NdmCreatedServices CreatedServices { get; }

            public Services(NdmRequiredServices requiredServices, NdmCreatedServices createdServices)
            {
                RequiredServices = requiredServices;
                CreatedServices = createdServices;
            }
        }
    }
}