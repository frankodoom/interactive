// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509 // don't warn on incomplete pattern matches
namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public MagicCommandTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        [Fact]
        public async Task magic_command_parse_errors_are_displayed()
        {
            var command = new Command("#!oops")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!oops");

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("Required argument missing for command: #!oops");
        }

        [Fact]
        public async Task magic_command_parse_errors_prevent_code_submission_from_being_run()
        {
            var command = new Command("#!x")
            {
                new Argument<string>()
            };

            using var kernel = new CSharpKernel();

            kernel.AddDirective(command);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!x\n123");

            events.Should().NotContain(e => e is ReturnValueProduced);
        }

        [Fact]
        public void Magic_commands_with_duplicate_aliases_are_not_allowed()
        {
            using var kernel = new CompositeKernel();

            kernel.AddDirective(new Command("#dupe"));

            kernel.Invoking(k => 
                k.AddDirective(new Command("#dupe")))
                  .Should()
                  .Throw<ArgumentException>()
                  .Which
                  .Message
                  .Should()
                  .Be("Alias \'#dupe\' is already in use.");
        }
    }
}