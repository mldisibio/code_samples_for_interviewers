//using System;
//using contoso.ado.Internals;
//using Newtonsoft.Json;
//using Npgsql;

//namespace contoso.ado.PostgreSql
//{
//    /// <summary>
//    /// <see cref="Npgsql.TypeMapping.INpgsqlTypeMapper"/>Pass-thru helper class to reduce caller code dependency on 'Npgsql' namespaces.
//    /// </summary>
//    public class GlobalTypeMapper
//    {
//        readonly PostgresDatabase _dbCtx;
//        readonly static JsonSerializerSettings _jsonDbSetting = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };

//        internal GlobalTypeMapper(PostgresDatabase postgresDb)
//        {
//            _dbCtx = postgresDb.ThrowIfNull();
//        }

//        /// <summary>Maps a CLR type to a PostgreSQL composite type.</summary>
//        /// <param name="pgCompositeType">
//        /// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
//        /// will translate the CLR type (e.g. 'MyClass') to its snake case equivalent (e.g. 'my_class')
//        /// </param>
//        public PostgresDatabase MapComposite<T>(string pgCompositeType = null)
//        {
//            NpgsqlConnection.GlobalTypeMapper.MapComposite<T>(pgCompositeType);
//            return _dbCtx;
//        }

//        /// <summary>Maps a CLR enum to a PostgreSQL enum type.</summary>
//        /// <param name="pgEnumType">
//        /// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
//        /// will translate the enum type (e.g. 'MyEnum') to its snake case equivalent (e.g. 'my_enum')
//        /// </param>
//        public PostgresDatabase MapEnum<TEnum>(string pgEnumType = null)
//            where TEnum : struct, Enum
//        {
//            NpgsqlConnection.GlobalTypeMapper.MapEnum<TEnum>(pgEnumType);
//            return _dbCtx;
//        }

//        /// <summary>Removes an existing mapping from this mapper. Attempts to read or write this type after removal will result in an exception.</summary>
//        /// <param name="pgTypeName">A PostgreSQL type name for the type in the database.</param>
//        public PostgresDatabase RemoveMapping<T>(string pgTypeName)
//        {
//            NpgsqlConnection.GlobalTypeMapper.UnmapComposite<T>(pgTypeName);
//            return _dbCtx;
//        }

//        /// <summary>Resets all mapping changes performed on this type mapper and reverts it to its original, starting state.</summary>
//        public PostgresDatabase Reset()
//        {
//            NpgsqlConnection.GlobalTypeMapper.Reset();
//            return _dbCtx;
//        }

//        /// <summary>Removes an existing composite type mapping.</summary>
//        /// <param name="pgCompositeType">
//        /// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
//        /// will translate the CLR type (e.g. 'MyClass') to its snake case equivalent (e.g. 'my_class')
//        /// </param>
//        public PostgresDatabase UnmapComposite<T>(string pgCompositeType = null)
//        {
//            NpgsqlConnection.GlobalTypeMapper.UnmapComposite<T>(pgCompositeType);
//            return _dbCtx;
//        }

//        /// <summary>Removes an existing enum mapping.</summary>
//        /// <param name="pgEnumType">
//        /// The PostgreSQL type name for the composite type. If null, the SnakeCaseNameTranslator
//        /// will translate the enum type (e.g. 'MyEnum') to its snake case equivalent (e.g. 'my_enum')
//        /// </param>
//        public PostgresDatabase UnmapEnum<TEnum>(string pgEnumType = null)
//            where TEnum : struct, Enum
//        {
//            NpgsqlConnection.GlobalTypeMapper.UnmapEnum<TEnum>(pgEnumType);
//            return _dbCtx;
//        }

//        /// <summary>
//        /// Specify one or more types for which Json.Net mappings to json or jsonb should be precompiled.
//        /// A type can be a CLR type and also an array type, such as <c>typeof(int[])</c>.
//        /// </summary>
//        public PostgresDatabase UseJsonNet(Type[] clrTypes)
//        {
//            if (clrTypes._IsNotNullOrEmpty())
//            {
//                NpgsqlConnection.GlobalTypeMapper.UseJsonNet(jsonbClrTypes: clrTypes, jsonClrTypes: clrTypes, settings: _jsonDbSetting);
//            }
//            return _dbCtx;
//        }

//        /// <summary>Sets up NodaTime mappings for the PostgreSQL date/time types.</summary>
//        public PostgresDatabase UseNodaTime()
//        {
//            NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
//            return _dbCtx;
//        }

//    }
//}
