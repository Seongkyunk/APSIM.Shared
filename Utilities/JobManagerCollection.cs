namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// This job manager encapsulates a collection of job managers.
    /// </summary>
    public class JobManagerCollection : IJobManager
    {
        /// <summary>The collection of jobs.</summary>
        private List<JobManagerWrapper> jobWrappers = new List<JobManagerWrapper>();

        /// <summary>Index into the jobs list to pull the next job from.</summary>
        private int jobIndex;

        /// <summary>Constructor</summary>
        /// <param name="jobsToRun">A list of jobs to run.</param>
        public JobManagerCollection(List<IJobManager> jobsToRun)
        {
            foreach (var jobManager in jobsToRun)
                jobWrappers.Add(new JobManagerWrapper(jobManager));
            jobIndex = 0;
        }

        /// <summary>Return the next job to run or null if nothing to run.</summary>
        /// <returns>Job to run or null if no more.</returns>
        public IRunnable GetNextJobToRun()
        {
            if (jobIndex < jobWrappers.Count)
            {
                IRunnable jobToRun;
                do
                {
                    jobToRun = jobWrappers[jobIndex].GetNextJobToRun();
                    if (jobToRun == null && jobIndex < jobWrappers.Count)
                    {
                        lock (this)
                            if (jobWrappers[jobIndex].AllJobsHaveBeenGenerated)
                                jobIndex++;
                    }
                }
                while (jobToRun == null && jobIndex < jobWrappers.Count);

                return jobToRun;
            }
            else
                return null;
        }

        /// <summary>
        /// Job has been competed. 
        /// </summary>
        /// <param name="args"></param>
        void IJobManager.JobCompleted(JobCompleteArgs args)
        {
            var jobWrapper = jobWrappers.Find(j => j.ContainsJob(args.job));
            jobWrapper.JobCompleted(args);
        }

        /// <summary>
        /// All jobs have been completed.
        /// </summary>
        /// <param name="args"></param>
        void IJobManager.AllCompleted(AllCompletedArgs args)
        {
            
        }

        /// <summary>
        /// Associates a job manager with the running jobs.
        /// </summary>
        private class JobManagerWrapper
        {
            private IJobManager jobManager;
            private List<IRunnable> runningJobs = new List<IRunnable>();
            private bool allCompletedHasBeenCalled;

            /// <summary>Constructor.</summary>
            /// <param name="manager">Job manager.</param>
            public JobManagerWrapper(IJobManager manager)
            {
                jobManager = manager;
            }

            /// <summary>Have all jobs been generated?</summary>
            public bool AllJobsHaveBeenGenerated { get; private set; }

            /// <summary>Does this instance contain the specified job?</summary>
            /// <param name="job">The job to search for.</param>
            public bool ContainsJob(IRunnable job)
            {
                lock (this)
                    return runningJobs.Contains(job);
            }

            /// <summary>Return the next job to run or null if nothing to run.</summary>
            /// <returns>Job to run or null if no more.</returns>
            public IRunnable GetNextJobToRun()
            {
                var job = jobManager.GetNextJobToRun();
                if (job == null)
                {
                    AllJobsHaveBeenGenerated = true;
                    AllComplete();
                }
                else
                    lock (this)
                        runningJobs.Add(job);
                return job;
            }

            public void JobCompleted(JobCompleteArgs args)
            {
                jobManager.JobCompleted(args);
                lock (this)
                    runningJobs.Remove(args.job);
                AllComplete();
            }

            public void AllComplete()
            {
                if (AllJobsHaveBeenGenerated && runningJobs.Count == 0)
                {
                    lock (this)
                        if (!allCompletedHasBeenCalled)
                        {
                            allCompletedHasBeenCalled = true;
                            jobManager.AllCompleted(new AllCompletedArgs());
                        }
                }
            }
        }
    }
}
