using CaseOnline.Azure.WebJobs.Extensions.Mqtt.Config;
using Microsoft.Extensions.Logging;
using Moq;
using MQTTnet;
using MQTTnet.ManagedClient;
using MQTTnet.Protocol;
using Xunit;
using CaseOnline.Azure.WebJobs.Extensions.Mqtt.Bindings;
using System.Linq;
using CaseOnline.Azure.WebJobs.Extensions.Mqtt.Tests.Util;
using System;
using Microsoft.Azure.WebJobs;
using System.Collections.Generic;

namespace CaseOnline.Azure.WebJobs.Extensions.Mqtt.Tests
{
    public class AttributeToConfigConverterTests
    {
        private readonly Mock<ILogger> _mockLogger = new Mock<ILogger>();
        private readonly MockNameResolver _resolver = new MockNameResolver();

        [Fact]
        public void ValidConfigurationIsMappedCorrect()
        {
            // Arrange 
            var mqttTriggerAttribute = new MqttTriggerAttribute(new[] { "testTopic" })
            {
                ConnectionString = "Server=ServerName;Port=1883;Username=UserName;Password=Password;ClientId=TestClientId"
            };

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act
            var result = attributeToConfigConverter.GetMqttConfiguration();

            // Assert  
            Assert.Equal(mqttTriggerAttribute.Topics, result.Topics.Select(x => x.Topic));
            Assert.Equal("TestClientId", result.Options.ClientOptions.ClientId);
        }

        [Fact]
        public void InvalidPortThrowsException()
        {
            // Arrange 
            var mqttTriggerAttribute = new MqttTriggerAttribute("testTopic")
            {
                ConnectionString = "Server=ServerName;Port=ByeWorld;Username=UserName;Password=Password"
            };

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<FormatException>(() => attributeToConfigConverter.GetMqttConfiguration());
        }

        [Fact]
        public void NoClientIdGuidBasedClientIdIsGenerated()
        {
            // Arrange 
            var mqttTriggerAttribute = new MqttTriggerAttribute(new[] { "testTopic" })
            {
                ConnectionString = "Server=ServerName;Port=1883;Username=UserName;Password=Password;ClientId="
            };

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act 
            var result = attributeToConfigConverter.GetMqttConfiguration();

            // Assert
            Assert.NotNull(result.Options.ClientOptions.ClientId);
            Assert.True(Guid.TryParse(result.Options.ClientOptions.ClientId, out var guid));
        }

        [Fact]
        public void NoServernameProvidedResultsInException()
        {
            // Arrange 
            var mqttTriggerAttribute = new MqttTriggerAttribute(new[] { "testTopic" })
            {
                ConnectionString = "Server=;Port=1883;Username=UserName;Password=Password;ClientId="
            };

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<Exception>(() => attributeToConfigConverter.GetMqttConfiguration());

        }

        [Fact]
        public void CustomConfigProviderIsInvoked()
        {
            // Arrange  
            var mqttTriggerAttribute = new MqttTriggerAttribute(typeof(TestMqttConfigProvider));

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act 
            var result = attributeToConfigConverter.GetMqttConfiguration();

            // Assert
            Assert.NotNull(result);
            Assert.Contains(result.Topics.Select(x => x.Topic), (x) => x == "Test");
        }


        [Fact]
        public void InvalidCustomConfigCreatorThrowsException()
        {
            // Arrange  
            var mqttTriggerAttribute = new MqttTriggerAttribute(typeof(string));

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidCustomConfigCreatorException>(() => attributeToConfigConverter.GetMqttConfiguration());
        }

        [Fact]
        public void BrokenCustomConfigCreatorThrowsException()
        {
            // Arrange  
            var mqttTriggerAttribute = new MqttTriggerAttribute(typeof(BrokenTestMqttConfigProvider));

            var attributeToConfigConverter = new AttributeToConfigConverter(mqttTriggerAttribute, _resolver, _mockLogger.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidCustomConfigCreatorException>(() => attributeToConfigConverter.GetMqttConfiguration());
        }
    }

    public class TestMqttConfigProvider : ICreateMqttConfig
    {
        public MqttConfig Create(INameResolver nameResolver, ILogger logger)
        {
            return new TestMqttConfig(new ManagedMqttClientOptions(), new[] { new TopicFilter("Test", MqttQualityOfServiceLevel.AtMostOnce) });
        }
    }

    public class TestMqttConfig : MqttConfig
    {
        public override IManagedMqttClientOptions Options { get; }

        public override IEnumerable<TopicFilter> Topics { get; }

        public TestMqttConfig(IManagedMqttClientOptions options, IEnumerable<TopicFilter> topics)
        {
            Options = options;
            Topics = topics;
        }
    }


    public class BrokenTestMqttConfigProvider : ICreateMqttConfig
    {
        public MqttConfig Create(INameResolver nameResolver, ILogger logger)
        {
            throw new NotImplementedException();
        }
    }
}
