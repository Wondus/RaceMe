using System.Security.Cryptography;
using System.Text;
using API.Models;
using Microsoft.Data.Sqlite;

namespace API;

public class Database
{
    private static string _connectionString = "Data Source=users.db";
    public Database()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS users (
        isAdmin INTEGER,
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT NOT NULL UNIQUE,
        email TEXT NOT NULL,
        password_hash TEXT NOT NULL,
        profile_photo BLOB,
        bio TEXT
        );
    ";
        command.ExecuteNonQuery();
        
        command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS user_matches (
        user_id INTEGER NOT NULL,
        seen_user_id INTEGER NOT NULL,
        liked BOOLEAN NOT NULL,
        PRIMARY KEY (user_id, seen_user_id),
        FOREIGN KEY (user_id) REFERENCES users(id),
        FOREIGN KEY (seen_user_id) REFERENCES users(id)
            );";
        
        command.ExecuteNonQuery();
        
        
        command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS matches (
        user_a INTEGER NOT NULL,
        user_b INTEGER NOT NULL,
        matched_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        PRIMARY KEY (user_a, user_b),
        FOREIGN KEY (user_a) REFERENCES users(id),
        FOREIGN KEY (user_b) REFERENCES users(id)
          );";
        command.ExecuteNonQuery();
        
        command = connection.CreateCommand();
        command.CommandText = @"
        CREATE TABLE IF NOT EXISTS reported_users (
        id INTEGER PRIMARY KEY,
        username TEXT NOT NULL,
        profile_photo BLOB,
        bio TEXT
        );
        ";
        command.ExecuteNonQuery();
        
        // create an admin user if not exists
        var checkAdminCmd = connection.CreateCommand();
        checkAdminCmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = 'admin'";
        var exists = Convert.ToInt32(checkAdminCmd.ExecuteScalar());

        if (exists == 0)
        {
            var insertAdminCmd = connection.CreateCommand();
            insertAdminCmd.CommandText = @"
        INSERT INTO users (isAdmin, username, email, password_hash)
        VALUES (1, 'admin', 'admin@example.com', $p)";
            insertAdminCmd.Parameters.AddWithValue("$p", HashPassword("123"));
            insertAdminCmd.ExecuteNonQuery();
        }
    }


    private string HashPassword(string password)
    {
        HashAlgorithm hashAlg = HashAlgorithm.Create("md5");
        if (hashAlg is null)
        {
            throw new Exception("Error when creating a hashing algorithm");
        }
        if (password == null) throw new ArgumentNullException(nameof(password));
        return hashAlg.ComputeHash(Encoding.UTF8.GetBytes(password)).ToString();
    }

    public bool Exists(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users WHERE username = $u";
        command.Parameters.AddWithValue("$u", username);

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }

    public bool Create(RegisterRequest request)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO users (isAdmin, username, email, password_hash) VALUES ($a, $u, $e, $p)";
        command.Parameters.AddWithValue("$a", 0); // can't create an andmin account with a normal login request
        command.Parameters.AddWithValue("$u", request.Username);
        command.Parameters.AddWithValue("$e", request.Email);
        command.Parameters.AddWithValue("$p", HashPassword(request.Password));

        command.ExecuteNonQuery();
        return true;
    }
    
    public bool UpdateUsername(int userId, string newUsername)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE users SET username = $u WHERE id = $id";
        command.Parameters.AddWithValue("$u", newUsername);
        command.Parameters.AddWithValue("$id", userId);

        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdatePassword(int userId, string newPassword)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE users SET password_hash = $p WHERE id = $id";
        command.Parameters.AddWithValue("$p", HashPassword(newPassword));
        command.Parameters.AddWithValue("$id", userId);

        return command.ExecuteNonQuery() > 0;
    }
    
    public bool UpdateProfilePhoto(int userId, byte[] photo)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE users SET profile_photo = $p WHERE id = $id";
        command.Parameters.AddWithValue("$p", photo);
        command.Parameters.AddWithValue("$id", userId);

        return command.ExecuteNonQuery() > 0;
    }

    public bool UpdateBio(int userId, string newBio)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE users SET bio = $b WHERE id = $id";
        command.Parameters.AddWithValue("$b", newBio);
        command.Parameters.AddWithValue("$id", userId);
        
        return command.ExecuteNonQuery() > 0;
    }
    
    
    public User? GetUser(LoginRequest loginRequest)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, profile_photo, bio, isAdmin FROM users WHERE username = $u AND password_hash = $p";
        command.Parameters.AddWithValue("$u", loginRequest.Username);
        command.Parameters.AddWithValue("$p", HashPassword(loginRequest.Password));

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                ImageBytes = !reader.IsDBNull(2) ? (byte[])reader["profile_photo"] : null,
                Bio = !reader.IsDBNull(3) ? reader.GetString(3) : String.Empty,
                isAdmin = reader.GetInt32(4)
            };
        }

        return null;
    }
    
    
    public bool MarkUserSeen(int userId, int seenUserId, bool liked)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
        INSERT OR REPLACE INTO user_matches (user_id, seen_user_id, liked)
        VALUES ($userId, $seenUserId, $liked)";
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$seenUserId", seenUserId);
        command.Parameters.AddWithValue("$liked", liked);

        return command.ExecuteNonQuery() > 0;
    }

    
    public List<User> GetUnseenUsers(int userId)
    {
        var users = new List<User>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT id, username, email, profile_photo, bio
        FROM users
        WHERE id != $id
        AND id NOT IN (
        SELECT seen_user_id FROM user_matches WHERE user_id = $id
         )";
        command.Parameters.AddWithValue("$id", userId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            users.Add(new User {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                Email = !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty,
                ImageBytes = !reader.IsDBNull(3) ? (byte[])reader["profile_photo"] : null,
                Bio = !reader.IsDBNull(4) ? reader.GetString(4) : string.Empty
            });
        }

        return users;
    }

    public List<User> GetMatches(int userId)
    {
        var list = new List<User>();
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        SELECT u.id, u.username, u.profile_photo, u.bio
        FROM matches m
        JOIN users u
        ON (m.user_a = $id AND u.id = m.user_b)
        OR (m.user_b = $id AND u.id = m.user_a)";
        cmd.Parameters.AddWithValue("$id", userId);
        using var rdr = cmd.ExecuteReader();
        while (rdr.Read())
        {
            list.Add(new User {
                Id = rdr.GetInt32(0),
                Username = rdr.GetString(1),
                ImageBytes = !rdr.IsDBNull(2) ? (byte[])rdr["profile_photo"] : null,
                Bio = !rdr.IsDBNull(3) ? rdr.GetString(3) : string.Empty
            });
        }
        return list;
    }
    
    public bool IsMutualMatch(int a, int b)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
        SELECT 1
        FROM user_matches m1
        JOIN user_matches m2
        ON m1.user_id = m2.seen_user_id
        AND m1.seen_user_id = m2.user_id
        WHERE m1.user_id = $a
        AND m1.seen_user_id = $b
        AND m1.liked = 1
        AND m2.liked = 1
      LIMIT 1";
        cmd.Parameters.AddWithValue("$a", a);
        cmd.Parameters.AddWithValue("$b", b);
        using var rdr = cmd.ExecuteReader();
        return rdr.Read();
    }
    
    public bool ReportUser(User user)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
      INSERT OR IGNORE INTO reported_users 
        (id, username, profile_photo, bio)
      VALUES ($id, $name, $photo, $bio)";
        command.Parameters.AddWithValue("$id", user.Id);
        command.Parameters.AddWithValue("$name", user.Username ?? string.Empty);

        // If ImageBytes is null, bind to DBNull.Value instead
        if (user.ImageBytes is byte[] bytes && bytes.Length > 0)
        {
            command.Parameters.AddWithValue("$photo", bytes);
        }
        else
        {
            command.Parameters.AddWithValue("$photo", DBNull.Value);
        }

        command.Parameters.AddWithValue("$bio", user.Bio ?? string.Empty);

        int rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }
    public List<User> GetReportedUsers()
    {
        var reportedUsers = new List<User>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT id, username, profile_photo, bio
        FROM reported_users
    ";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            reportedUsers.Add(new User
            {
                Id = reader.GetInt32(0),
                Username = reader.GetString(1),
                ImageBytes = !reader.IsDBNull(2) ? (byte[])reader["profile_photo"] : null,
                Bio = !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty
            });
        }

        return reportedUsers;
    }

    /// <summary>
    /// Completely deletes a user and all related records.
    /// </summary>
    public bool DeleteUser(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var transaction = connection.BeginTransaction();

        // 1) Remove from user_matches (both as source and as target)
        var cmdUserMatches = connection.CreateCommand();
        cmdUserMatches.CommandText = @"
            DELETE FROM user_matches
            WHERE user_id = $id OR seen_user_id = $id
        ";
        cmdUserMatches.Parameters.AddWithValue("$id", userId);
        cmdUserMatches.ExecuteNonQuery();

        // 2) Remove from matches (both as user_a and user_b)
        var cmdMatches = connection.CreateCommand();
        cmdMatches.CommandText = @"
            DELETE FROM matches
            WHERE user_a = $id OR user_b = $id
        ";
        cmdMatches.Parameters.AddWithValue("$id", userId);
        cmdMatches.ExecuteNonQuery();

        // 3) Remove from reported_users
        var cmdReported = connection.CreateCommand();
        cmdReported.CommandText = @"
            DELETE FROM reported_users
            WHERE id = $id
        ";
        cmdReported.Parameters.AddWithValue("$id", userId);
        cmdReported.ExecuteNonQuery();

        // 4) Delete from users
        var cmdUsers = connection.CreateCommand();
        cmdUsers.CommandText = @"
            DELETE FROM users
            WHERE id = $id
        ";
        cmdUsers.Parameters.AddWithValue("$id", userId);
        var rows = cmdUsers.ExecuteNonQuery();

        transaction.Commit();
        return rows > 0;
    }

    /// <summary>
    /// Removes a user from the reported_users table
    /// </summary>
    public bool RemoveReportedUser(int userId)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM reported_users
            WHERE id = $id
        ";
        cmd.Parameters.AddWithValue("$id", userId);
        return cmd.ExecuteNonQuery() > 0;
    }
    
}