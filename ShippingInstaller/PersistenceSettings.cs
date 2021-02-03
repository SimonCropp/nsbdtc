using NServiceBus.Persistence.Sql;

[assembly: SqlPersistenceSettings(
    MsSqlServerScripts = true,
    MySqlScripts = false,
    OracleScripts = false,
    ProduceTimeoutScripts = false,
    ProduceOutboxScripts = false,
    ProduceSubscriptionScripts = false,
    ScriptPromotionPath = "$(ProjectDir)PromotedSqlScripts"
)]