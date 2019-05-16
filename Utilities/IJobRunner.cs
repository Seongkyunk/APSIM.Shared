namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>Arguments for JobComplete event</summary>
    public class JobCompleteArgs
    {
        /// <summary>The job that was completed</summary>
        public IRunnable job;

        /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
        public Exception exceptionThrowByJob;
    }

    /// <summary>Arguments for AllJobsCompleted event</summary>
    public class AllCompletedArgs
    {
        /// <summary>The exceptions (if any) thrown by the worker thread. Can be null for no exceptions.</summary>
        public List<Exception> exceptionsThrown;
    }

    /// <summary>An interface for running jobs</summary>
    public interface IJobRunner
    {
        /// <summary>Invoked when all jobs have completed</summary>
        event EventHandler<AllCompletedArgs> AllJobsCompleted;

        /// <summary>Invoked when a job has completed</summary>
        event EventHandler<JobCompleteArgs> JobCompleted;

        /// <summary>Run the specified jobs</summary>
        /// <param name="jobs">An instance of a class that manages all jobs.</param>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        /// <param name="numberOfProcessors">The maximum number of cores to use.</param>
        void Run(IJobManager jobs, bool wait = false, int numberOfProcessors = -1);

        /// <summary>Stop all jobs currently running</summary>
        void Stop();
    }
}