﻿

namespace Shared.Database
{
    // Niveau d'isolation des Transactions
    // Transaction Isolation Level
    public enum IsolationLevel
    {
        DEFAULT,
        SERIALIZABLE,
        REPEATABLE_READ,
        READ_COMMITTED,
        READ_UNCOMMITTED,
        SNAPSHOT
    }
}