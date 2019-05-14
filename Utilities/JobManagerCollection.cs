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
        private List<JobManagerWrapper> jobs = new List<JobManagerWrapper>();

        /// <summary>Index into the jobs list to pull the next job from.</summary>
        private int jobIndex;

        /// <summary>Constructor</summary>
        /// <param name="jobsToRun">A list of jobs to run.</param>
        public JobManagerCollection(List<IJobManager> jobsToRun)
        {
            foreach (var jobManager in jobsToRun)
                jobs.Add(new JobManagerWrapper() { JobManager = jobManager });
            jobIndex = 0;
        }

        /// <summary>Return the next job to run or null if nothing to run.</summary>
        /// <returns>Job to run or null if no more.</returns>
        public IRunnable GetNextJobToRun()
        {
            if (jobIndex < jobs.Count)
            {
                var jobToRun = jobs[jobIndex].JobManager.GetNextJobToRun();
                while (jobToRun == null && jobIndex < jobs.Count)
                {
                    jobs[jobIndex].AllJobsHaveBeenGenerated = true;
                    jobs[jobIndex].AllComplete();
                    jobIndex++;
                    if (jobIndex < jobs.Count)
                        jobToRun = jobs[jobIndex].JobManager.GetNextJobToRun();
                }

                if (jobToRun != null)
                    jobs[jobIndex].RunningJobs.Add(jobToRun);
                return jobToRun;
            }
            else
                return null;
        }

        void IJobManager.JobCompleted(JobCompleteArgs args)
        {
            var job = jobs.Find(j => j.RunningJobs.Contains(args.job));
            job.JobManager.JobCompleted(args);
            lock (job)
            {
                job.JobCompleted(args);
            }
        }

        void IJobManager.AllCompleted(AllCompletedArgs args)
        {
            
        }

        /// <summary>
        /// Associates a job manager with the running jobs.
        /// </summary>
        private class JobManagerWrapper
        {
            public IJobManager JobManager { get; set; }
            public List<IRunnable> RunningJobs { get; set; } = new List<IRunnable>();
            public bool AllJobsHaveBeenGenerated;

            public void JobCompleted(JobCompleteArgs args)
            {
                RunningJobs.Remove(args.job);
                AllComplete();
            }

            public void AllComplete()
            {
                if (RunningJobs.Count == 0 && AllJobsHaveBeenGenerated)
                {
                    JobManager.AllCompleted(new AllCompletedArgs());
                }
            }
        }
    }
}
