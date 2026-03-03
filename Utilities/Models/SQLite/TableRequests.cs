namespace IDC.AggrMapping.Utilities.Models.SQLite;

/// <summary>
/// Request model for creating a new SQLite table with specified columns and their data types
/// </summary>
/// <remarks>
/// Used to specify table name and column definitions when creating a new table.
/// Column names are dictionary keys, and their SQLite data types are the values.
///
/// Features:
/// - Supports all SQLite data types
/// - Allows primary key definition
/// - Enables column constraints
/// - Handles unique indexes
/// - Supports nullable columns
///
/// Example:
/// <code>
/// {
///   "tableName": "users",
///   "columns": {
///     "id": "INTEGER PRIMARY KEY",
///     "name": "TEXT NOT NULL",
///     "email": "TEXT UNIQUE",
///     "age": "INTEGER NULL",
///     "created_at": "DATETIME DEFAULT CURRENT_TIMESTAMP"
///   }
/// }
/// </code>
///
/// > [!IMPORTANT]
/// > Table names should follow SQLite naming conventions and avoid reserved keywords
///
/// > [!TIP]
/// > Consider adding appropriate constraints to maintain data integrity
///
/// > [!NOTE]
/// > Column names are case-sensitive in queries but not in definitions
/// </remarks>
/// <seealso cref="AlterTableRequest"/>
/// <seealso href="https://www.sqlite.org/datatype3.html">SQLite Data Types</seealso>
/// <seealso href="https://www.sqlite.org/lang_createtable.html">SQLite CREATE TABLE</seealso>
public class CreateTableRequest
{
    /// <summary>
    /// Name of the table to be created in SQLite database
    /// </summary>
    /// <remarks>
    /// The table name must follow SQLite naming conventions:
    /// - Start with a letter or underscore
    /// - Can contain letters, numbers, and underscores
    /// - Case-insensitive
    /// - Maximum length of 128 characters
    ///
    /// Example:
    /// <code>
    /// var request = new CreateTableRequest
    /// {
    ///     TableName = "users_profile"
    /// };
    /// </code>
    ///
    /// > [!IMPORTANT]
    /// > Avoid using SQLite reserved keywords as table names
    ///
    /// > [!NOTE]
    /// > While table names are case-insensitive in SQLite, it's recommended to use lowercase with underscores
    /// </remarks>
    /// <returns>Returns the current instance to enable method chaining</returns>
    /// <seealso href="https://www.sqlite.org/lang_createtable.html">SQLite CREATE TABLE Documentation</seealso>
    /// <seealso href="https://www.sqlite.org/lang_keywords.html">SQLite Reserved Keywords</seealso>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary containing column definitions for the SQLite table where key is column name and value is the SQLite data type with constraints
    /// </summary>
    /// <remarks>
    /// Defines the structure of table columns including their data types and constraints.
    ///
    /// Supported SQLite data types:
    /// - INTEGER: Whole numbers
    /// - REAL: Floating point numbers
    /// - TEXT: String values
    /// - BLOB: Binary data
    /// - DATETIME: Date and time values
    /// - BOOLEAN: True/false values (stored as INTEGER)
    ///
    /// Common constraints:
    /// - PRIMARY KEY: Unique identifier
    /// - NOT NULL: Required value
    /// - UNIQUE: Unique value across rows
    /// - DEFAULT: Default value
    /// - CHECK: Custom validation
    ///
    /// Example:
    /// <code>
    /// var columns = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["id"] = "INTEGER PRIMARY KEY AUTOINCREMENT",
    ///     ["email"] = "TEXT NOT NULL UNIQUE",
    ///     ["created_at"] = "DATETIME DEFAULT CURRENT_TIMESTAMP",
    ///     ["status"] = "TEXT CHECK(status IN ('active','inactive'))",
    ///     ["age"] = "INTEGER CHECK(age >= 18)",
    ///     ["profile_data"] = "BLOB NULL"
    /// };
    /// </code>
    ///
    /// > [!IMPORTANT]
    /// > Column names should be lowercase with underscores for consistency
    ///
    /// > [!TIP]
    /// > Use appropriate constraints to maintain data integrity
    ///
    /// > [!NOTE]
    /// > SQLite uses dynamic typing - type affinity determines storage class
    /// </remarks>
    /// <returns>Returns the current instance to enable method chaining</returns>
    /// <seealso href="https://www.sqlite.org/datatype3.html">SQLite Data Types</seealso>
    /// <seealso href="https://www.sqlite.org/lang_createtable.html#tableconstraints">SQLite Table Constraints</seealso>
    /// <seealso href="https://www.sqlite.org/stricttables.html">SQLite Strict Tables</seealso>
    public Dictionary<string, string> Columns { get; set; } = [];
}

/// <summary>
/// Request model for altering an existing SQLite table structure
/// </summary>
/// <remarks>
/// Provides functionality to modify table structure by adding or removing columns.
/// Supports atomic operations for table alterations.
///
/// Features:
/// - Add new columns with constraints
/// - Remove existing columns
/// - Supports all SQLite data types
/// - Maintains data integrity during alterations
///
/// Example:
/// <code>
/// var alterRequest = new AlterTableRequest
/// {
///     ColumnsToAdd = new Dictionary&lt;string, string&gt;
///     {
///         ["phone"] = "TEXT NOT NULL DEFAULT ''",
///         ["address"] = "TEXT NULL",
///         ["last_modified"] = "DATETIME DEFAULT CURRENT_TIMESTAMP"
///     },
///     ColumnsToRemove = new List&lt;string&gt;
///     {
///         "old_phone",
///         "temp_field"
///     }
/// };
/// </code>
///
/// > [!IMPORTANT]
/// > Adding columns with NOT NULL constraint requires a DEFAULT value
///
/// > [!CAUTION]
/// > Removing columns is irreversible and will delete all data in those columns
///
/// > [!TIP]
/// > Consider creating a backup before performing table alterations
/// </remarks>
/// <seealso href="https://www.sqlite.org/lang_altertable.html">SQLite ALTER TABLE Documentation</seealso>
/// <seealso href="https://www.sqlite.org/datatype3.html">SQLite Data Types</seealso>
/// <seealso cref="CreateTableRequest"/>
public class AlterTableRequest
{
    /// <summary>
    /// Dictionary of columns to add where key is column name and value is column type/definition
    /// </summary>
    /// <remarks>
    /// Specifies new columns to be added to an existing table with their corresponding SQLite data types and constraints.
    ///
    /// Features:
    /// - Supports all SQLite data types
    /// - Allows column constraints
    /// - Enables default values
    /// - Handles nullable columns
    ///
    /// Example:
    /// <code>
    /// var columnsToAdd = new Dictionary&lt;string, string&gt;
    /// {
    ///     ["phone_number"] = "TEXT NOT NULL DEFAULT '+'",
    ///     ["is_verified"] = "BOOLEAN DEFAULT 0",
    ///     ["last_login"] = "DATETIME NULL",
    ///     ["login_count"] = "INTEGER DEFAULT 0 CHECK(login_count >= 0)",
    ///     ["metadata"] = "BLOB"
    /// };
    /// </code>
    ///
    /// > [!IMPORTANT]
    /// > Columns with NOT NULL constraint must specify a DEFAULT value
    ///
    /// > [!NOTE]
    /// > Column names should use lowercase with underscores for consistency
    ///
    /// > [!TIP]
    /// > Consider adding appropriate constraints to maintain data integrity
    /// </remarks>
    /// <returns>Returns the current instance to enable method chaining</returns>
    /// <seealso href="https://www.sqlite.org/lang_altertable.html">SQLite ALTER TABLE Documentation</seealso>
    /// <seealso href="https://www.sqlite.org/datatype3.html">SQLite Data Types</seealso>
    /// <seealso href="https://www.sqlite.org/lang_createtable.html#tableconstraints">SQLite Column Constraints</seealso>
    public Dictionary<string, string>? ColumnsToAdd { get; set; }

    /// <summary>
    /// List of column names to be removed from the existing SQLite table
    /// </summary>
    /// <remarks>
    /// Specifies which columns should be dropped from the table structure.
    /// The operation is permanent and will result in data loss for the specified columns.
    ///
    /// Features:
    /// - Supports multiple column removal in single operation
    /// - Maintains referential integrity
    /// - Preserves remaining column data
    /// - Handles index adjustments automatically
    ///
    /// Example:
    /// <code>
    /// var request = new AlterTableRequest
    /// {
    ///     ColumnsToRemove = new List&lt;string&gt;
    ///     {
    ///         "deprecated_field",
    ///         "temporary_column",
    ///         "unused_metadata"
    ///     }
    /// };
    /// </code>
    ///
    /// > [!WARNING]
    /// > This operation permanently deletes column data and cannot be undone
    ///
    /// > [!IMPORTANT]
    /// > Cannot remove PRIMARY KEY columns or columns referenced by foreign keys
    ///
    /// > [!TIP]
    /// > Create a backup before removing columns from production databases
    /// </remarks>
    /// <returns>Returns the current instance to enable method chaining</returns>
    /// <seealso href="https://www.sqlite.org/lang_altertable.html">SQLite ALTER TABLE Documentation</seealso>
    /// <seealso cref="ColumnsToAdd"/>
    public List<string>? ColumnsToRemove { get; set; }
}
