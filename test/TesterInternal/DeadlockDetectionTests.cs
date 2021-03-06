using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;
using Orleans.TestingHost;
using UnitTests.GrainInterfaces;
using Xunit;
using Tester;
using TestExtensions;

namespace UnitTests.General
{
    public class DeadlockDetectionTests : OrleansTestingBase, IClassFixture<DeadlockDetectionTests.Fixture>
    {
        private readonly Fixture fixture;

        public class Fixture : BaseTestClusterFixture
        {
            protected override void ConfigureTestCluster(TestClusterBuilder builder)
            {
                builder.ConfigureLegacyConfiguration(legacy =>
                {
                    legacy.ClusterConfiguration.Globals.PerformDeadlockDetection = true;
                    legacy.ClusterConfiguration.Globals.AllowCallChainReentrancy = true;
                });
            }
        }

        private const int numIterations = 30;

        public DeadlockDetectionTests(Fixture fixture)
        {
            this.fixture = fixture;
        }

        // 2 silos, loop across all cases (to force all grains to be local and remote):
        //      Non Reentrant A, B
        //      Reentrant C
        // 1) Allowed reentrancy A, A
        // 2) Allowed reentrancy A, B, A
        // 3) Deadlock C, A, C, A
        // 4) No Deadlock C, C
        // 5) No Deadlock C, A, C

        // 1) Allowed reentrancy A, A
        [Fact, TestCategory("Functional"), TestCategory("Deadlock")]
        public async Task DeadlockDetection_1()
        {
            long baseGrainId = random.Next();
            for (int i = 0; i < numIterations; i++)
            {
                long grainId = baseGrainId + i;
                IDeadlockNonReentrantGrain firstGrain = this.fixture.GrainFactory.GetGrain<IDeadlockNonReentrantGrain>(grainId);
                List<Tuple<long, bool>> callChain = new List<Tuple<long, bool>>();
                callChain.Add(new Tuple<long, bool>(grainId, true));
                callChain.Add(new Tuple<long, bool>(grainId, true));
                await firstGrain.CallNext_1(callChain, 1);
            }
        }

        // 2) Allowed reentrancy on non-reentrant grains A, B, A
        [Fact, TestCategory("Functional"), TestCategory("Deadlock")]
        public async Task DeadlockDetection_2()
        {
            long baseGrainId = random.Next();
            long bBase = 100;
            for (int i = 0; i < numIterations; i++)
            {
                long grainId = baseGrainId + i;
                IDeadlockNonReentrantGrain firstGrain = this.fixture.GrainFactory.GetGrain<IDeadlockNonReentrantGrain>(grainId);
                List<Tuple<long, bool>> callChain = new List<Tuple<long, bool>>();
                callChain.Add(new Tuple<long, bool>(grainId, true));
                callChain.Add(new Tuple<long, bool>(bBase + grainId, true));
                callChain.Add(new Tuple<long, bool>(grainId, true));
                await firstGrain.CallNext_1(callChain, 1);
            }
        }

        // 3) Allowed reentrancy C, A, C, A
        [Fact, TestCategory("Functional"), TestCategory("Deadlock")]
        public async Task DeadlockDetection_3()
        {
            long baseGrainId = random.Next();
            long cBase = 200;
            for (int i = 0; i < numIterations; i++)
            {
                long grainId = baseGrainId + i;
                IDeadlockReentrantGrain firstGrain = this.fixture.GrainFactory.GetGrain<IDeadlockReentrantGrain>(grainId);
                List<Tuple<long, bool>> callChain = new List<Tuple<long, bool>>();
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));
                callChain.Add(new Tuple<long, bool>(grainId, true));
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));
                callChain.Add(new Tuple<long, bool>(grainId, true));
                await firstGrain.CallNext_1(callChain, 1);
            }
        }

        // 4) No Deadlock C, C
        [Fact, TestCategory("Functional"), TestCategory("Deadlock")]
        public async Task DeadlockDetection_4()
        {
            long baseGrainId = random.Next();
            long cBase = 200;
            for (int i = 0; i < numIterations; i++)
            {
                long grainId = baseGrainId + i;
                IDeadlockReentrantGrain firstGrain = this.fixture.GrainFactory.GetGrain<IDeadlockReentrantGrain>(grainId);
                List<Tuple<long, bool>> callChain = new List<Tuple<long, bool>>();
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));

                await firstGrain.CallNext_1(callChain, 1);
            }
        }

        // 5) No Deadlock C, A, C
        [Fact, TestCategory("Functional"), TestCategory("Deadlock")]
        public async Task DeadlockDetection_5()
        {
            long baseGrainId = random.Next();
            long cBase = 200;
            for (int i = 0; i < numIterations; i++)
            {
                long grainId = baseGrainId + i;
                IDeadlockReentrantGrain firstGrain = this.fixture.GrainFactory.GetGrain<IDeadlockReentrantGrain>(grainId);
                List<Tuple<long, bool>> callChain = new List<Tuple<long, bool>>();
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));
                callChain.Add(new Tuple<long, bool>(grainId, true));
                callChain.Add(new Tuple<long, bool>(cBase + grainId, false));

                await firstGrain.CallNext_1(callChain, 1);
            }
        }
    }
}
