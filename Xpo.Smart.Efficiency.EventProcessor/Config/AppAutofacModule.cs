using Autofac;
using Xpo.Smart.Efficiency.Engine.Interfaces;
using Xpo.Smart.Efficiency.Engine.Providers.Transactions;
using Xpo.Smart.Efficiency.EventProcessor.Events;
using Xpo.Smart.Efficiency.Shared.DataCache.Core;
using Xpo.Smart.Efficiency.Shared.DataCache.Transactions;

namespace Xpo.Smart.Efficiency.EventProcessor.Config
{
    public class AppAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<InitEfficiencyRunner>()
                .As<IInitEfficiencyRunner>()
                .SingleInstance();

            builder.Register<ITransactionProvider>(c =>
            {
                var transactionCacheFactory = c.Resolve<ICacheRepositoryFactory<ITransactionCacheRepository>>();
                return new TransactionCacheTransactionProvider(transactionCacheFactory);
            });
        }
    }
}
