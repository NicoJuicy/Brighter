﻿#region Licence
/* The MIT License (MIT)
Copyright © 2014 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the “Software”), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Paramore.Brighter.MessagingGateway.RMQ;
using Xunit;

namespace Paramore.Brighter.RMQ.Tests.MessagingGateway;

[Trait("Category", "RMQ")]
public class RmqMessageProducerQueueLengthTestsAsync : IDisposable, IAsyncDisposable
{
    private readonly IAmAMessageProducerAsync _messageProducer;
    private readonly IAmAMessageConsumerAsync _messageConsumer;
    private readonly Message _messageOne;
    private readonly Message _messageTwo;
    private readonly ChannelName _queueName = new(Guid.NewGuid().ToString());

    public RmqMessageProducerQueueLengthTestsAsync()
    {
        var routingKey = new RoutingKey(Guid.NewGuid().ToString());
            
        _messageOne = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, 
                MessageType.MT_COMMAND), 
            new MessageBody("test content"));
           
        _messageTwo = new Message(
            new MessageHeader(Guid.NewGuid().ToString(), routingKey, 
                MessageType.MT_COMMAND), 
            new MessageBody("test content"));

        var rmqConnection = new RmqMessagingGatewayConnection
        {
            AmpqUri = new AmqpUriSpecification(new Uri("amqp://guest:guest@localhost:5672/%2f")),
            Exchange = new Exchange("paramore.brighter.exchange"),
        };
            
        _messageProducer = new RmqMessageProducer(rmqConnection);

        _messageConsumer = new RmqMessageConsumer(
            connection: rmqConnection, 
            queueName: _queueName, 
            routingKey: routingKey, 
            isDurable: false, 
            highAvailability: false,
            batchSize: 5,
            maxQueueLength: 1,
            makeChannels:OnMissingChannel.Create
        );
             
    }

    [Fact]
    public async Task When_rejecting_a_message_due_to_queue_length()
    {
        //create the infrastructure
        await _messageConsumer.ReceiveAsync(TimeSpan.Zero); 
            
        await _messageProducer.SendAsync(_messageOne);
        await _messageProducer.SendAsync(_messageTwo);

        //check messages are flowing - absence needs to be expiry
        var messages = await _messageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(5000));
        var message = messages.First();
        await _messageConsumer.AcknowledgeAsync(message);
            
        //should be the first message
            
        //try to grab the next message
        var nextMessages = await _messageConsumer.ReceiveAsync(TimeSpan.FromMilliseconds(5000));
        message = nextMessages.First();
        message.Header.MessageType.Should().Be(MessageType.MT_NONE);

    }

    public void Dispose()
    {
        ((IAmAMessageProducerSync)_messageProducer).Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _messageProducer.DisposeAsync(); 
    }
}