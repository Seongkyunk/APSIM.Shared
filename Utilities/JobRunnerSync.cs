// -----------------------------------------------------------------------
// <copyright file="JobRunnerSync.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A class for running jobs synchronously.</summary>
    public class JobRunnerSync : IJobRunner
    {
        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler<AllCompletedArgs> AllJobsCompleted;

        /// <summary>Invoked when a job is completed.</summary>
        public event EventHandler<JobCompleteArgs> JobCompleted;

        /// <summary>A token for cancelling running of jobs</summary>
        private CancellationTokenSource cancelToken;

        /// <summary>The background task.</summary>
        private Task backgroundTask;

        /// <summary>Have all jobs been completed?</summary>
        private bool allJobsFinished;

        /// <summary>Run the specified jobs</summary>
        /// <param name="jobs">An instance of a class that manages all jobs.</param>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        /// <param name="numberOfProcessors">The maximum number of cores to use.</param>
        public void Run(IJobManager jobs, bool wait = false, int numberOfProcessors = -1)
        {
            cancelToken = new CancellationTokenSource();
            allJobsFinished = false;

            // Run all jobs on background thread
            backgroundTask = Task.Run(() => JobRunnerThread(jobs));

            if (wait)
                while (!backgroundTask.IsCompleted)
                    Thread.Sleep(200);
        }

        /// <summary>Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.        /// </summary>
        /// <param name="jobs">An instance of a class that manages all jobs.</param>
        private void JobRunnerThread(IJobManager jobs)
        {
            // No - get another job to run.
            IRunnable job = jobs.GetNextJobToRun();

            Exception exceptionThrown = null;
            try
            {
                // Iterate through all jobs and run them.
                while (job != null && !cancelToken.IsCancellationRequested)
                {
                    JobCompleteArgs jobCompleteArguments = new JobCompleteArgs();
                    jobCompleteArguments.job = job;
                    try
                    {
                        job.Run(cancelToken);
                    }
                    catch (Exception err)
                    {
                        jobCompleteArguments.exceptionThrowByJob = err;
                    }
                    if (JobCompleted != null)
                        JobCompleted.Invoke(this, jobCompleteArguments);
                    jobs.JobCompleted(jobCompleteArguments);

                    job = jobs.GetNextJobToRun();
                }
            }
            catch (Exception err)
            {
                exceptionThrown = err;
            }

            var args = new AllCompletedArgs();
            if (exceptionThrown != null)
                args.exceptionsThrown = new List<Exception>() { exceptionThrown };
            jobs.AllCompleted(args);
            allJobsFinished = true;
            AllJobsCompleted?.Invoke(this, args);
        }

        /// <summary>Stop all jobs currently running</summary>
        public void Stop()
        {
            if (!allJobsFinished)
            {
                cancelToken.Cancel();
                while (!allJobsFinished)
                    Thread.Sleep(200);
            }
        }
        
    }
}
