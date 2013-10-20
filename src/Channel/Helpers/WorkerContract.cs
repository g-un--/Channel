using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Channel.Helpers
{
    class WorkerContract<T>
    {
        private readonly Worker<T> worker;
        private readonly IDisposable policy;

        public Worker<T> Worker { get { return worker; } }
        public IDisposable Policy { get { return policy; } }

        public WorkerContract(Worker<T> worker, IDisposable policy)
        {
            if (worker == null)
            {
                throw new ArgumentNullException("worker");
            }
            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            this.worker = worker;
            this.policy = policy;
        }
    }
}
