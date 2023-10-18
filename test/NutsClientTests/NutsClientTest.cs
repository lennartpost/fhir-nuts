using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nuts.Plugin;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace NutsClientTests
{
    public class NutsClientTest
    {
        private const string BGZ_SENDER = "Customer-One";
        private const string BGZ_RECEIVER = "Customer-Two";
        private const string SENDER_FHIR_ENDPOINT = "http://fhir-one:4080";
        private const string RECEIVER_NOTIFICATION_ENDPOINT = "http://fhir-two:4080";



        private readonly ILogger<NutsClient> _logger;
        private readonly NutsClient _senderClient;
        private readonly NutsClient _receiverClient;

        public NutsClientTest(ITestOutputHelper testOutputHelper)
        {
            _logger = XUnitLogger.CreateLogger<NutsClient>(testOutputHelper);
            _senderClient = new NutsClient(_logger, Options.Create(new NutsOptions() { OrganizationName = BGZ_SENDER, NodeUrl = "http://localhost:1323" }));
            _receiverClient = new NutsClient(_logger, Options.Create(new NutsOptions() { OrganizationName = BGZ_RECEIVER, NodeUrl = "http://localhost:2323" }));
        }

        [Fact]
        public async Task GetSenderDidByOrganizationNameTest()
        {
            var (senderDid, _) = await getSenderAndReceiverDidAsync();
            Assert.Equal("did:nuts:BsfT3tMRDsMvH46tqURmLuHh6NBe5YzuUHoFRkmNtXve", senderDid);
        }

        [Fact]
        public async Task GetReceiverDidByOrganizationNameTest()
        {
            var (_, receiverDid) = await getSenderAndReceiverDidAsync();
            Assert.Equal("did:nuts:5scTBPtKSpenxRsv441WR176wq6R4ZxmxusM93UHgXQZ", receiverDid);
        }

        [Fact]
        public async Task GetSenderFhirEndpointTest()
        {
            var (senderDid, _) = await getSenderAndReceiverDidAsync();
            var senderFhirEndpoint = await _senderClient.GetEndpointAsync(senderDid, "bgz-sender", "fhir");
            Assert.Equal(SENDER_FHIR_ENDPOINT, senderFhirEndpoint);
        }

        [Fact]
        public async Task GetReceiverNotificationEndpointTest()
        {
            var (_, receiverDid) = await getSenderAndReceiverDidAsync();
            var notificationEndpoint = await _senderClient.GetEndpointAsync(receiverDid, "bgz-receiver", "notification");
            Assert.Equal(RECEIVER_NOTIFICATION_ENDPOINT, notificationEndpoint);
        }

        [Fact]
        public async Task CreateAuthorizationCredentialsTest()
        {
            var (senderDid, receiverDid) = await getSenderAndReceiverDidAsync();
            var vcDid = await _senderClient.CreateAuthorizationCredentialsCredentialSubjectAsync(senderDid, receiverDid, 1);
            Assert.NotNull(vcDid);
            _logger.LogInformation("Created VC: {vcDid}", vcDid);
        }

        [Fact]
        public async Task GetAccessTokenTest()
        {
            var (senderDid, receiverDid) = await getSenderAndReceiverDidAsync();
            var accessToken = await _senderClient.GetAccessTokenAsync(receiverDid, senderDid);
            Assert.NotNull(accessToken);
            _logger.LogInformation("Retrieved access token: {accessToken}", accessToken);

            var introspectResult = await _receiverClient.IntrospectAccessTokenAsync(accessToken);
            Assert.NotNull(introspectResult);
            _logger.LogInformation("Access token introspection result: {introspectResult}", introspectResult);
        }

        private async Task<(string, string)> getSenderAndReceiverDidAsync()
        {
            var senderDid = await _senderClient.GetDidByOrganizationNameAsync(BGZ_SENDER);
            var receiverDid = await _senderClient.GetDidByOrganizationNameAsync(BGZ_RECEIVER);
            if (senderDid is null || receiverDid is null)
                Assert.Fail($"Sendir DID {senderDid} or receiver DID {receiverDid} is null");

            return (senderDid, receiverDid);
        }
    }
}