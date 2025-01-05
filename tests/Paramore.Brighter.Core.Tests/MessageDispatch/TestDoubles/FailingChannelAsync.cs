#region Licence
/* The MIT License (MIT)
Copyright © 2015 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
using System.Threading;
using System.Threading.Tasks;
using Polly.CircuitBreaker;

namespace Paramore.Brighter.Core.Tests.MessageDispatch.TestDoubles
{
    internal class FailingChannelAsync(ChannelName channelName, RoutingKey topic, IAmAMessageConsumerAsync messageConsumer, int maxQueueLength= 1, bool brokenCircuit = false)
        : ChannelAsync(channelName, topic, messageConsumer, maxQueueLength)
    {
        public int NumberOfRetries { get; set; } = 0;
        private int _attempts = 0;

        
        public override async Task<Message> ReceiveAsync(TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (_attempts <= NumberOfRetries)
            {
                _attempts++;
                var channelFailureException = new ChannelFailureException("Test general failure", new Exception("inner test exception"));
                if (brokenCircuit)
                {
                    var brokenCircuitException = new BrokenCircuitException("An inner broken circuit exception");
                    channelFailureException = new ChannelFailureException("Test broken circuit failure", brokenCircuitException);
                }

                throw channelFailureException;
            }

            return await base.ReceiveAsync(timeout, cancellationToken);
        }
    }
}