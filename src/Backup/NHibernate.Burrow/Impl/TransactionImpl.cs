using System;
using System.Transactions;
using log4net;

namespace NHibernate.Burrow.Impl
{
    internal class TransactionImpl : ITransaction
    {
        //  private NHibernate.ITransaction nhtransaction;
        private NHibernate.ISession session;
        private readonly Func<TransactionScope> transactionScopeFactory;
        private TransactionScope transactionScope;

        public TransactionImpl()
        {
            transactionScopeFactory = FrameworkEnvironment.Instance.Configuration.TransactionScopeFactory;
            if (transactionScopeFactory != null)
            {
                transactionScope = transactionScopeFactory();
            }
        }

        #region ITransaction Members

        /// <summary>
        /// 
        /// </summary>
        public void Begin(ISession sess)
        {
            session = sess;
            if (InTransaction)
            {
                throw new Exceptions.IncorrectTransactionStatusException("Transaction has already begun");
            }
            sess.BeginTransaction();
        }

        /// <summary>
        /// whether the transaction has begun and not committed yet
        /// </summary>
        public bool InTransaction
        {
            get
            {
                if (session == null) return false;
                return isValid(session.Transaction);
            }
        }

        private static bool isValid(NHibernate.ITransaction transaction)
        {
            if (transaction == null) return false;
            if (transaction.WasCommitted || transaction.WasRolledBack) return false;

            return transaction.IsActive;
        }

        public void Dispose()
        {
            try
            {
                if (transactionScope != null)
                    transactionScope.Dispose();
            }
            catch (Exception e)
            {
                //Catch the exception thrown from  to prevent the original exception from being swallowed.
                try
                {
                    ILog log = LogManager.GetLogger(typeof (SessionAndTransactionManager));
                    if (log.IsErrorEnabled)
                    {
                        log.Error("NHibernate.Burrow Dispose failed", e);
                    }
                    else
                    {
                        Console.WriteLine(e);
                    }
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                transactionScope = null;
            }
        }

        /// <summary>
        /// Try commit the nhtransaction, if failed the nhtransaction will be rollback and the session will be close
        /// </summary>
        public void Commit()
        {
            CheckInTransaction();

            try
            {
                if (isValid(session.Transaction))
                    session.Transaction.Commit();

                if (transactionScope != null)
                    transactionScope.Complete();
            }
            catch (Exception)
            {
                try
                {
                    Rollback();
                }
                catch (Exception e)
                {
                    //Catch the exception thrown from RollBackTransaction() to prevent the original exception from being swallowed.

                    ILog log = LogFactory.Log;
                    if (log.IsErrorEnabled)
                    {
                        log.Error("NHibernate.Burrow Rollback failed", e);
                    }
                    else
                    {
                        Console.WriteLine(e);
                    }
                }
                throw;
            }
        }

        private void CheckInTransaction()
        {
            if (!InTransaction)
                throw new Exceptions.IncorrectTransactionStatusException(
                    "It's not in transaction. Either you haven't started it or it's already close");
        }

        /// <summary>
        /// Rollback the Transaction and Close Session
        /// </summary>
        /// <remarks>
        /// if the tranasaction has already been rollback or the session closed this will do nothing. 
        /// You can perform this method multiple times, only the first time will take effect. 
        /// </remarks>
        public void Rollback()
        {
            try
            {
                var transaction = session.Transaction;
                if (isValid(transaction))
                {
                    transaction.Rollback();
                }
            }
            catch (Exception e)
            {
                //Catch the exception thrown from  to prevent the original exception from being swallowed.
                try
                {
                    ILog log = LogManager.GetLogger(typeof (SessionAndTransactionManager));
                    if (log.IsErrorEnabled)
                    {
                        log.Error("NHibernate.Burrow Rollback failed", e);
                    }
                    else
                    {
                        Console.WriteLine(e);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        #endregion
    }
}