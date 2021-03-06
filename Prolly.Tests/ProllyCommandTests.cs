﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Prolly.Tests.TestSupport;
using Prolly.Exceptions;
using System.Threading.Tasks;
using System.Threading;

namespace Prolly.Tests
{
    [TestClass]
    public class ProllyCommandTests
    {
        [TestInitialize()]
        public void Initialize()
        {
            Prolly.Reset();
        }

        [TestMethod]
        public void Execute_Returns_Run_Value()
        {
            // Arrange
            var sut = new TimeoutCommand(TimeSpan.FromMilliseconds(0));

            // Act
            string result = sut.Execute();

            // Assert
            Assert.AreEqual(TimeoutCommand.ReturnValue, result);
        }

        [TestMethod]
        public void ExecuteAsync_Returns_Run_Value()
        {
            // Arrange
            var sut = new TimeoutCommand(TimeSpan.FromMilliseconds(0));

            // Act
            string result = sut.ExecuteAsync().Result;

            // Assert
            Assert.AreEqual(TimeoutCommand.ReturnValue, result);
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void Execute_Throws_Exception_On_Timeout()
        {
            // Arrange
            var sut = new TimeoutCommand(TimeSpan.FromMilliseconds(4000));

            // Act
            string result = sut.Execute();

            // Assert
            Assert.AreEqual(TimeoutCommand.ReturnValue, result);
        }

        [TestMethod]
        public void Execute_Returns_Fallback_After_Timeout()
        {
            // Arrange
            var sut = new TimeoutCommandWithFallback(TimeSpan.FromMilliseconds(1200));

            // Act
            string result = sut.Execute();

            // Assert
            Assert.AreEqual(TimeoutCommandWithFallback.FallbackValue, result);
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public void Execute_No_Fallback_Trips_CircuitBreaker_After_Two_Fails()
        {
            // Arrange
            var sut = new TimeoutCommand(TimeSpan.FromMilliseconds(1200));

            // Act
            try { string result = sut.Execute(); }
            catch ( Exception ) { }

            try { string result = sut.Execute(); }
            catch ( Exception ) { }

            sut.Execute();

            // Assert
            // ExpectedException
        }

        [TestMethod]
        [ExpectedException(typeof(CircuitBreakerOpenException))]
        public void Execute_Different_Instance_Same_Command_Break_Ciruit()
        {
            // Arrange
            var timeoutCommand1 = new TimeoutCommand(TimeSpan.FromMilliseconds(1200));
            var timeoutCommand2 = new TimeoutCommand(TimeSpan.FromMilliseconds(1200));
            var timeoutCommand3 = new TimeoutCommand(TimeSpan.FromMilliseconds(400));

            // Act
            try { string result = timeoutCommand1.Execute(); }
            catch ( Exception ) { }

            try { string result = timeoutCommand2.Execute(); }
            catch ( Exception ) { }

            timeoutCommand3.Execute();

            // Assert
            // ExpectedException
        }

        [TestMethod, Timeout(4000)]
        public void Execute_CircuitBreaker_Opens_After_Succes_When_HalfOpen()
        {
            // Arrange
            var timeoutCommand = new TimeoutCommand(TimeSpan.FromMilliseconds(2000));
            var zeroTimeoutCommand = new TimeoutCommand(TimeSpan.FromMilliseconds(0));

            // Act
                // First close the CircuitBreaker
            try { string result = timeoutCommand.Execute(); }
            catch ( Exception ) { }
            try { string result = timeoutCommand.Execute(); }
            catch ( Exception ) { }
                // Wait for it to get in the HalfOpen state
            while ( !timeoutCommand.CircuitBreaker.AllowRequests )
            { }
                // So we are HalfOpen now
            zeroTimeoutCommand.Execute();

            // Assert
            Assert.IsTrue(zeroTimeoutCommand.CircuitBreaker.AllowRequests);
        }

        [TestMethod]
        public void Execute_Returns_Fallback_When_Open()
        {
            // Arrange
            var sut = new TimeoutCommandWithFallback(TimeSpan.FromMilliseconds(1200));

            // Act
            try { sut.Execute(); }
            catch ( Exception ) { }
            try { sut.Execute(); }
            catch ( Exception ) { }

            string result = sut.Execute();

            // Assert
            Assert.AreEqual(result, TimeoutCommandWithFallback.FallbackValue);
        }

        [TestMethod]
        public void CircuitBreakerIgnoreException_Doesnt_Trigger_CircuitBreaker()
        {
            // Arrange
            var sut = new CircuitBreakerIgnoreExceptionCommand();

            // Act
            try { sut.Execute(); }
            catch ( CircuitBreakerIgnoreException ) { }
            try { sut.Execute(); }
            catch ( CircuitBreakerIgnoreException ) { }
            try { sut.Execute(); }
            catch ( CircuitBreakerIgnoreException ) { }

            // Assert
            Assert.IsTrue(sut.CircuitBreaker.AllowRequests);
        }

        [TestMethod]
        [ExpectedException(typeof(MaximumAllowedTasksReachedException))]
        public void Bulkhead_Exception_After_Two_Running_Tasks()
        {
            // Arrange
            var sut = new TimeoutCommand(TimeSpan.FromMilliseconds(200));

            // Act
            Task t1 = Task<string>.Factory.StartNew(sut.Execute);
            Task t2 = Task<string>.Factory.StartNew(sut.Execute);
            sut.Execute();

            try
            {
                Task.WhenAll(t1, t2).Wait();
                Assert.Fail();
            } 
            catch (Exception ex)
            {
                throw ex.InnerException;
            }

            // Assert
            // ExpectedException
        }      
    }
}
