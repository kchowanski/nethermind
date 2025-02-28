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
using FluentAssertions;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Core.Model;
using Nethermind.Core.Test.Builders;
using Nethermind.DataMarketplace.Channels;
using Nethermind.DataMarketplace.Consumers.Domain;
using Nethermind.DataMarketplace.Consumers.Infrastructure.Rpc;
using Nethermind.DataMarketplace.Consumers.Infrastructure.Rpc.Models;
using Nethermind.DataMarketplace.Consumers.Queries;
using Nethermind.DataMarketplace.Consumers.Services;
using Nethermind.DataMarketplace.Core.Domain;
using Nethermind.DataMarketplace.Core.Services;
using Nethermind.DataMarketplace.Infrastructure.Rpc.Models;
using Nethermind.Facade;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Nethermind.DataMarketplace.Consumers.Test.Infrastructure
{
    public class NdmRpcConsumerModuleTests
    {
        private IConsumerService _consumerService;
        private IReportService _reportService;
        private IJsonRpcNdmConsumerChannel _jsonRpcNdmConsumerChannel;
        private IEthRequestService _ethRequestService;
        private IPersonalBridge _personalBridge;
        private INdmRpcConsumerModule _rpc;

        [SetUp]
        public void Setup()
        {
            _consumerService = Substitute.For<IConsumerService>();
            _reportService = Substitute.For<IReportService>();
            _jsonRpcNdmConsumerChannel = Substitute.For<IJsonRpcNdmConsumerChannel>();
            _ethRequestService = Substitute.For<IEthRequestService>();
            _personalBridge = Substitute.For<IPersonalBridge>();
            _rpc = new NdmRpcConsumerModule(_consumerService, _reportService, _jsonRpcNdmConsumerChannel,
                _ethRequestService, _personalBridge, LimboLogs.Instance);
        }

        [Test]
        public void module_type_should_be_ndm_consumer()
        {
            _rpc.ModuleType.Should().Be(ModuleType.NdmConsumer);
        }

        [Test]
        public void given_personal_bridge_list_accounts_should_return_accounts()
        {
            _personalBridge.ListAccounts().Returns(new[] {TestItem.AddressA});
            var result = _rpc.ndm_listAccounts();
            _personalBridge.Received().ListAccounts();
            result.Data.Should().ContainSingle();
            var account = result.Data.Single();
            account.Should().NotBeNull();
            account.Address.Should().NotBeNull();
            account.Unlocked.Should().BeFalse();
        }

        [Test]
        public void given_null_personal_bridge_list_accounts_should_not_return_accounts()
        {
            _personalBridge = null;
            _rpc = new NdmRpcConsumerModule(_consumerService, _reportService, _jsonRpcNdmConsumerChannel,
                _ethRequestService, _personalBridge, LimboLogs.Instance);
            var result = _rpc.ndm_listAccounts();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public void get_consumer_address_should_return_address()
        {
            _consumerService.GetAddress().Returns(TestItem.AddressA);
            var result = _rpc.ndm_getConsumerAddress();
            _consumerService.Received().GetAddress();
            result.Data.Should().Be(TestItem.AddressA);
        }

        [Test]
        public async Task change_consumer_address_should_return_changed_address()
        {
            var result = await _rpc.ndm_changeConsumerAddress(TestItem.AddressA);
            await _consumerService.Received().ChangeAddressAsync(TestItem.AddressA);
            result.Data.Should().Be(TestItem.AddressA);
        }

        [Test]
        public void get_discovered_data_headers_should_return_data_headers()
        {
            _consumerService.GetDiscoveredDataHeaders().Returns(new List<DataHeader> {GetDataHeader()});
            var result = _rpc.ndm_getDiscoveredDataHeaders();
            _consumerService.Received().GetDiscoveredDataHeaders();
            result.Data.Should().ContainSingle();
            VerifyDataHeader(result.Data.Single());
        }

        [Test]
        public async Task get_known_data_headers_should_return_data_header_info()
        {
            _consumerService.GetKnownDataHeadersAsync()
                .Returns(new[] {new DataHeaderInfo(Keccak.Zero, "test", "test")});
            var result = await _rpc.ndm_getKnownDataHeaders();
            await _consumerService.Received().GetKnownDataHeadersAsync();
            result.Data.Should().ContainSingle();
            var dataHeader = result.Data.Single();
            dataHeader.Id.Should().Be(Keccak.Zero);
            dataHeader.Name.Should().Be("test");
            dataHeader.Description.Should().Be("test");
        }

        [Test]
        public async Task get_known_providers_should_return_providers_info()
        {
            _consumerService.GetKnownProvidersAsync().Returns(new[] {new ProviderInfo("test", TestItem.AddressA)});
            var result = await _rpc.ndm_getKnownProviders();
            await _consumerService.Received().GetKnownProvidersAsync();
            result.Data.Should().ContainSingle();
            var provider = result.Data.Single();
            provider.Name.Should().Be("test");
            provider.Address.Should().Be(TestItem.AddressA);
        }

        [Test]
        public void get_connected_providers_should_return_providers_addresses()
        {
            _consumerService.GetConnectedProviders().Returns(new[] {TestItem.AddressA});
            var result = _rpc.ndm_getConnectedProviders();
            _consumerService.Received().GetConnectedProviders();
            result.Data.Should().ContainSingle();
            result.Data.Single().Should().Be(TestItem.AddressA);
        }

        [Test]
        public void get_active_consumer_sessions_should_return_sessions()
        {
            _consumerService.GetActiveSessions().Returns(new[] {GetConsumerSession()});
            var result = _rpc.ndm_getActiveConsumerSessions();
            _consumerService.Received().GetActiveSessions();
            result.Data.Should().ContainSingle();
            var session = result.Data.Single();
            VerifyConsumerSession(session);
        }

        [Test]
        public async Task get_deposits_should_return_paged_results_of_deposits()
        {
            var query = new GetDeposits();
            _consumerService.GetDepositsAsync(query)
                .Returns(PagedResult<DepositDetails>.Create(new[] {GetDepositDetails()}, 1, 1, 1, 1));
            var result = await _rpc.ndm_getDeposits(query);
            await _consumerService.Received().GetDepositsAsync(query);
            result.Data.Items.Should().ContainSingle();
            result.Data.Page.Should().Be(1);
            result.Data.Results.Should().Be(1);
            result.Data.TotalPages.Should().Be(1);
            result.Data.TotalResults.Should().Be(1);
            result.Data.IsEmpty.Should().BeFalse();
            VerifyDepositDetails(result.Data.Items.Single());
        }

        [Test]
        public async Task get_deposit_should_return_deposit()
        {
            var depositId = TestItem.KeccakA;
            _consumerService.GetDepositAsync(depositId).Returns(GetDepositDetails());
            var result = await _rpc.ndm_getDeposit(depositId);
            await _consumerService.Received().GetDepositAsync(depositId);
            result.Result.ResultType.Should().Be(ResultType.Success);
            result.ErrorType.Should().Be(ErrorType.None);
            result.Result.Error.Should().BeNullOrWhiteSpace();
            VerifyDepositDetails(result.Data);
        }
        
        [Test]
        public async Task get_deposit_should_fail_if_deposit_was_not_found()
        {
            var depositId = TestItem.KeccakA;
            var result = await _rpc.ndm_getDeposit(depositId);
            await _consumerService.Received().GetDepositAsync(depositId);
            result.Data.Should().BeNull();
            result.Result.ResultType.Should().Be(ResultType.Failure);
            result.ErrorType.Should().Be(ErrorType.InternalError);
            result.Result.Error.Should().NotBeNull();
        }
        
        [Test]
        public async Task make_deposit_should_return_deposit_id()
        {
            var request = new MakeDepositForRpc
            {
                DataHeaderId = Keccak.Zero,
                Units = 10,
                Value = 100
            };
            var depositId = TestItem.KeccakA;
            _consumerService.MakeDepositAsync(request.DataHeaderId, request.Units, request.Value).Returns(depositId);
            var result = await _rpc.ndm_makeDeposit(request);
            await _consumerService.Received().MakeDepositAsync(request.DataHeaderId, request.Units, request.Value);
            result.Data.Should().Be(depositId);
        }

        [Test]
        public async Task send_data_request_should_return_deposit_id()
        {
            var depositId = TestItem.KeccakA;
            _consumerService.SendDataRequestAsync(depositId).Returns(depositId);
            var result = await _rpc.ndm_sendDataRequest(depositId);
            await _consumerService.Received().SendDataRequestAsync(depositId);
            result.Data.Should().Be(depositId);
        }

        [Test] public async Task send_data_request_should_fail_if_deposit_was_not_found()
        {
            var depositId = TestItem.KeccakA;
            var result = await _rpc.ndm_sendDataRequest(depositId);
            await _consumerService.Received().SendDataRequestAsync(depositId);
            result.Data.Should().BeNull();
            result.Result.ResultType.Should().Be(ResultType.Failure);
            result.ErrorType.Should().Be(ErrorType.InternalError);
            result.Result.Error.Should().NotBeNull();
        }
        
        [Test]
        public async Task finish_session_should_return_deposit_id()
        {
            var depositId = TestItem.KeccakA;
            _consumerService.SendFinishSessionAsync(depositId).Returns(depositId);
            var result = await _rpc.ndm_finishSession(depositId);
            await _consumerService.Received().SendFinishSessionAsync(depositId);
            result.Data.Should().Be(depositId);
        }
        
        [Test]
        public async Task finish_session_should_fail_if_deposit_was_not_found()
        {
            var depositId = TestItem.KeccakA;
            var result = await _rpc.ndm_finishSession(depositId);
            await _consumerService.Received().SendFinishSessionAsync(depositId);
            result.Data.Should().BeNull();
            result.Result.ResultType.Should().Be(ResultType.Failure);
            result.ErrorType.Should().Be(ErrorType.InternalError);
            result.Result.Error.Should().NotBeNull();
        }
        
        [Test]
        public async Task enable_data_stream_should_return_deposit_id()
        {
            var depositId = TestItem.KeccakA;
            var args = new[] {"test"};
            _consumerService.EnableDataStreamAsync(depositId, args).Returns(depositId);
            var result = await _rpc.ndm_enableDataStream(depositId, args);
            await _consumerService.Received().EnableDataStreamAsync(depositId, args);
            result.Data.Should().Be(depositId);
        }
        
        [Test]
        public async Task enable_data_stream_should_fail_if_deposit_was_not_found()
        {
            var depositId = TestItem.KeccakA;
            var args = new[] {"test"};
            var result = await _rpc.ndm_enableDataStream(depositId, args);
            await _consumerService.Received().EnableDataStreamAsync(depositId, args);
            result.Data.Should().BeNull();
            result.Result.ResultType.Should().Be(ResultType.Failure);
            result.ErrorType.Should().Be(ErrorType.InternalError);
            result.Result.Error.Should().NotBeNull();
        }
        
        [Test]
        public async Task disable_data_stream_should_return_deposit_id()
        {
            var depositId = TestItem.KeccakA;
            _consumerService.DisableDataStreamAsync(depositId).Returns(depositId);
            var result = await _rpc.ndm_disableDataStream(depositId);
            await _consumerService.Received().DisableDataStreamAsync(depositId);
            result.Data.Should().Be(depositId);
        }
        
        [Test]
        public async Task disable_data_stream_should_fail_if_deposit_was_not_found()
        {
            var depositId = TestItem.KeccakA;
            var result = await _rpc.ndm_disableDataStream(depositId);
            await _consumerService.Received().DisableDataStreamAsync(depositId);
            result.Data.Should().BeNull();
            result.Result.ResultType.Should().Be(ResultType.Failure);
            result.ErrorType.Should().Be(ErrorType.InternalError);
            result.Result.Error.Should().NotBeNull();
        }

        [Test]
        public async Task get_deposits_report_should_return_report()
        {
            var query = new GetDepositsReport();
            var item = GetDepositReportItem();
            var report = new DepositsReport(1, 1, 0,
                PagedResult<DepositReportItem>.Create(new[] {item}, 1, 1, 1, 1));
            _reportService.GetDepositsReportAsync(query).Returns(report);
            var result = await _rpc.ndm_getDepositsReport(query);
            await _reportService.Received().GetDepositsReportAsync(query);
            result.Data.Should().NotBeNull();
            result.Data.Deposits.Should().NotBeNull();
            result.Data.Deposits.Items.Should().ContainSingle();
            result.Data.Deposits.Page.Should().Be(1);
            result.Data.Deposits.Results.Should().Be(1);
            result.Data.Deposits.TotalPages.Should().Be(1);
            result.Data.Deposits.TotalResults.Should().Be(1);
            result.Data.Deposits.IsEmpty.Should().BeFalse();
            result.Data.ClaimedValue.Should().Be(1);
            result.Data.RefundedValue.Should().Be(0);
            result.Data.RemainingValue.Should().Be(0);
            result.Data.TotalValue.Should().Be(1);
            VerifyDepositReportItem(result.Data.Deposits.Items.Single(), item);
        }

        [Test]
        public async Task get_deposit_approvals_should_return_approvals()
        {
            var query = new GetConsumerDepositApprovals();
            var approval = new DepositApproval(Keccak.Zero, TestItem.KeccakA, "test", "kyc",
                TestItem.AddressA, TestItem.AddressB, 1);
            _consumerService.GetDepositApprovalsAsync(query)
                .Returns(PagedResult<DepositApproval>.Create(new[] {approval}, 1, 1, 1, 1));
            var result = await _rpc.ndm_getConsumerDepositApprovals(query);
            await _consumerService.Received().GetDepositApprovalsAsync(query);
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().NotBeNull();
            result.Data.Items.Should().ContainSingle();
            result.Data.Page.Should().Be(1);
            result.Data.Results.Should().Be(1);
            result.Data.TotalPages.Should().Be(1);
            result.Data.TotalResults.Should().Be(1);
            result.Data.IsEmpty.Should().BeFalse();
            var rpcApproval = result.Data.Items.Single();
            rpcApproval.HeaderId.Should().Be(approval.HeaderId);
            rpcApproval.HeaderName.Should().Be(approval.HeaderName);
            rpcApproval.Kyc.Should().Be(approval.Kyc);
            rpcApproval.Consumer.Should().Be(approval.Consumer);
            rpcApproval.Provider.Should().Be(approval.Provider);
            rpcApproval.Timestamp.Should().Be(approval.Timestamp);
        }

        [Test]
        public async Task request_deposit_approval_should_return_id()
        {
            var id = TestItem.KeccakA;
            _consumerService.RequestDepositApprovalAsync(Keccak.Zero, "kyc").Returns(id);
            var result = await _rpc.ndm_requestDepositApproval(Keccak.Zero, "kyc");
            await _consumerService.Received().RequestDepositApprovalAsync(Keccak.Zero, "kyc");
            result.Data.Should().Be(id);
        }
        
        [Test]
        public async Task request_deposit_approval_should_fail_if_header_was_not_found()
        {
            var result = await _rpc.ndm_requestDepositApproval(Keccak.Zero, "kyc");
            await _consumerService.Received().RequestDepositApprovalAsync(Keccak.Zero, "kyc");
            result.Data.Should().BeNull();
        }
        
        [Test]
        public async Task request_eth_should_return_true()
        {
            var address = TestItem.AddressA;
            var value = 1.Ether();
            _ethRequestService.TryRequestEthAsync(address, value).Returns(FaucetResponse.RequestCompleted(null));
            var result = await _rpc.ndm_requestEth(address);
            await _ethRequestService.Received().TryRequestEthAsync(address, value);
            result.Data.Status.Should().Be(FaucetRequestStatus.RequestCompleted.ToString());
        }
        
        [Test]
        public async Task request_eth_should_fail_if_request_cannot_be_made()
        {
            var address = TestItem.AddressA;
            var value = 1.Ether();
            _ethRequestService.TryRequestEthAsync(address, value).Returns(FaucetResponse.RequestError);
            var result = await _rpc.ndm_requestEth(address);
            await _ethRequestService.Received().TryRequestEthAsync(address, value);
            result.Data.Status.Should().Be(FaucetRequestStatus.RequestError.ToString());
        }
        
        [Test]
        public void pull_data_should_return_data()
        {
            var depositId = Keccak.Zero;
            const string data = "data";
            _jsonRpcNdmConsumerChannel.Pull(depositId).Returns(data);
            var result = _rpc.ndm_pullData(depositId);
            _jsonRpcNdmConsumerChannel.Received().Pull(depositId);
            result.Data.Should().Be(data);
        }

        private static void VerifyDepositReportItem(DepositReportItemForRpc rpcItem, DepositReportItem item)
        {
            rpcItem.Id.Should().Be(item.Id);
            rpcItem.HeaderId.Should().Be(item.HeaderId);
            rpcItem.HeaderName.Should().Be(item.HeaderName);
            rpcItem.Provider.Should().Be(item.Provider);
            rpcItem.ProviderName.Should().Be(item.ProviderName);
            rpcItem.Value.Should().Be(item.Value);
            rpcItem.Units.Should().Be(item.Units);
            rpcItem.Completed.Should().Be(item.Completed);
            rpcItem.Timestamp.Should().Be(item.Timestamp);
            rpcItem.ExpiryTime.Should().Be(item.ExpiryTime);
            rpcItem.Expired.Should().Be(item.Expired);
            rpcItem.TransactionHash.Should().Be(item.TransactionHash);
            rpcItem.VerificationTimestamp.Should().Be(item.VerificationTimestamp);
            rpcItem.Verified.Should().Be(item.Verified);
            rpcItem.ClaimedRefundTransactionHash.Should().Be(item.ClaimedRefundTransactionHash);
            rpcItem.RefundClaimed.Should().Be(item.RefundClaimed);
            rpcItem.ConsumedUnits.Should().Be(item.ConsumedUnits);
            rpcItem.ClaimedUnits.Should().Be(item.ClaimedUnits);
            rpcItem.RefundedUnits.Should().Be(item.RefundedUnits);
            rpcItem.RemainingUnits.Should().Be(item.RemainingUnits);
            rpcItem.ClaimedValue.Should().Be(item.ClaimedValue);
            rpcItem.RefundedValue.Should().Be(item.RefundedValue);
            rpcItem.RemainingValue.Should().Be(item.RemainingValue);
            rpcItem.Receipts.Should().ContainSingle();
            item.Receipts.Should().ContainSingle();
            VerifyDataDeliveryReceiptReportItem(rpcItem.Receipts.Single(), item.Receipts.Single());
        }

        private static void VerifyDataDeliveryReceiptReportItem(DataDeliveryReceiptReportItemForRpc rpcReceipt,
            DataDeliveryReceiptReportItem receipt)
        {
            rpcReceipt.Id.Should().Be(receipt.Id);
            rpcReceipt.Number.Should().Be(receipt.Number);
            rpcReceipt.SessionId.Should().Be(receipt.SessionId);
            rpcReceipt.NodeId.Should().Be(receipt.NodeId);
            rpcReceipt.Timestamp.Should().Be(receipt.Timestamp);
            rpcReceipt.IsMerged.Should().Be(receipt.IsMerged);
            rpcReceipt.IsClaimed.Should().Be(receipt.IsClaimed);
            rpcReceipt.Request.Number.Should().Be(receipt.Request.Number);
            rpcReceipt.Request.DepositId.Should().Be(receipt.Request.DepositId);
            rpcReceipt.Request.UnitsRange.From.Should().Be(receipt.Request.UnitsRange.From);
            rpcReceipt.Request.UnitsRange.To.Should().Be(receipt.Request.UnitsRange.To);
            rpcReceipt.Request.IsSettlement.Should().Be(receipt.Request.IsSettlement);
            rpcReceipt.Receipt.StatusCode.Should().Be(receipt.Receipt.StatusCode.ToString().ToLowerInvariant());
            rpcReceipt.Receipt.ConsumedUnits.Should().Be(receipt.Receipt.ConsumedUnits);
            rpcReceipt.Receipt.UnpaidUnits.Should().Be(receipt.Receipt.UnpaidUnits);
        }
        
        private static void VerifyConsumerSession(ConsumerSessionForRpc session)
        {
            session.Id.Should().Be(Keccak.Zero);
            session.DepositId.Should().Be(TestItem.KeccakA);
            session.DataHeaderId.Should().Be(TestItem.KeccakB);
            session.ConsumerAddress.Should().Be(TestItem.AddressA);
            session.ConsumerNodeId.Should().Be(TestItem.PublicKeyA);
            session.ProviderAddress.Should().Be(TestItem.AddressB);
            session.ProviderNodeId.Should().Be(TestItem.PublicKeyB);
            session.State.Should().Be(SessionState.Started.ToString().ToLowerInvariant());
            session.StartUnitsFromConsumer.Should().Be(0);
            session.StartUnitsFromProvider.Should().Be(0);
            session.StartTimestamp.Should().Be(0);
            session.FinishTimestamp.Should().Be(0);
            session.ConsumedUnits.Should().Be(0);
            session.UnpaidUnits.Should().Be(0);
            session.PaidUnits.Should().Be(0);
            session.SettledUnits.Should().Be(0);
            session.ConsumedUnitsFromProvider.Should().Be(0);
            session.DataAvailability.Should().Be(DataAvailability.Available.ToString().ToLowerInvariant());
            session.StreamEnabled.Should().BeFalse();
            session.Args.Should().BeEmpty();
        }

        private static void VerifyDepositDetails(DepositDetailsForRpc deposit)
        {
            deposit.Id.Should().Be(Keccak.OfAnEmptyString);
            deposit.Deposit.Should().NotBeNull();
            deposit.Deposit.Id.Should().Be(Keccak.OfAnEmptyString);
            deposit.Deposit.Units.Should().Be(1);
            deposit.Deposit.Value.Should().Be(1);
            deposit.Deposit.ExpiryTime.Should().Be(1);
            deposit.Timestamp.Should().Be(1);
            deposit.TransactionHash.Should().Be(TestItem.KeccakA);
            deposit.Verified.Should().Be(false);
            deposit.RefundClaimed.Should().Be(false);
            deposit.ClaimedRefundTransactionHash.Should().BeNull();
            deposit.ConsumedUnits.Should().Be(0);
            deposit.Kyc.Should().BeNullOrEmpty();
            VerifyDataHeader(deposit.DataHeader);
        }

        private static void VerifyDataHeader(DataHeaderForRpc dataHeader)
        {
            dataHeader.Should().NotBeNull();
            dataHeader.Id.Should().NotBeNull();
            dataHeader.Name.Should().Be("test");
            dataHeader.Description.Should().Be("test");
            dataHeader.UnitPrice.Should().Be(1);
            dataHeader.UnitType.Should().Be(DataHeaderUnitType.Unit.ToString().ToLowerInvariant());
            dataHeader.QueryType.Should().Be(QueryType.Stream.ToString().ToLowerInvariant());
            dataHeader.MinUnits.Should().Be(0);
            dataHeader.MaxUnits.Should().Be(10);
            dataHeader.Rules.Should().NotBeNull();
            dataHeader.Rules.Expiry.Should().NotBeNull();
            dataHeader.Rules.Expiry.Value.Should().Be(1);
            dataHeader.Rules.UpfrontPayment.Should().BeNull();
            dataHeader.Provider.Address.Should().Be(Address.Zero);
            dataHeader.Provider.Name.Should().Be("test");
            dataHeader.File.Should().BeNullOrEmpty();
            dataHeader.Data.Should().BeNullOrEmpty();
            dataHeader.State.Should().Be(DataHeaderState.Unpublished.ToString().ToLowerInvariant());
            dataHeader.TermsAndConditions.Should().BeNullOrEmpty();
            dataHeader.KycRequired.Should().BeFalse();
        }

        private static ConsumerSession GetConsumerSession()
            => new ConsumerSession(Keccak.Zero, TestItem.KeccakA, TestItem.KeccakB, TestItem.AddressA,
                TestItem.PublicKeyA, TestItem.AddressB, TestItem.PublicKeyB, SessionState.Started,
                0, 0, dataAvailability: DataAvailability.Available);

        private static DataHeader GetDataHeader()
            => new DataHeader(Keccak.OfAnEmptyString, "test", "test", 1,
                DataHeaderUnitType.Unit, 0, 10, new DataHeaderRules(new DataHeaderRule(1)),
                new DataHeaderProvider(Address.Zero, "test"));

        private static DepositDetails GetDepositDetails()
            => new DepositDetails(new Deposit(Keccak.OfAnEmptyString, 1, 1, 1),
                GetDataHeader(), Array.Empty<byte>(), 1, TestItem.KeccakA);

        private static DepositReportItem GetDepositReportItem()
            => new DepositReportItem(Keccak.Zero, TestItem.KeccakA, "test", TestItem.AddressA,
                "test", 1, 1, 1, 1, false, TestItem.KeccakA,
                1, TestItem.KeccakB, 1, new[]
                {
                    new DataDeliveryReceiptReportItem(Keccak.Zero, 1, TestItem.KeccakC, TestItem.PublicKeyA,
                        new DataDeliveryReceiptRequest(1, TestItem.KeccakD, new UnitsRange(0, 1), false,
                            new[] {new DataDeliveryReceiptToMerge(new UnitsRange(0, 1), new Signature(0, 0, 27))}),
                        new DataDeliveryReceipt(StatusCodes.Ok, 1, 1, new Signature(0, 0, 27)), 1, false, false)
                });
    }
}