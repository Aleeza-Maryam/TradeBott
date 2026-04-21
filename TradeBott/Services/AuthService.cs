using System;
using System.Threading.Tasks;
using TradeBot.Interfaces;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Handles User Authentication: Login and Signup logic
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IPortfolioRepository _portfolioRepo;

        public AuthService(
            IUserRepository userRepo,
            IPortfolioRepository portfolioRepo)
        {
            _userRepo = userRepo;
            _portfolioRepo = portfolioRepo;
        }

        // Creates a new user account and an associated portfolio
        public async Task<Tuple<bool, string, User>> SignupAsync(
            string username, string password, string email)
        {
            // Input Validation
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return Tuple.Create(false,
                    "Username must be at least 3 characters long",
                    (User)null);

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return Tuple.Create(false,
                    "Password must be at least 6 characters long",
                    (User)null);

            // Check if username already exists
            if (await _userRepo.UsernameExistsAsync(username))
                return Tuple.Create(false,
                    $"Username '{username}' is already taken",
                    (User)null);

            // Hash the password using BCrypt for security
            string hashedPassword = BCrypt.Net.BCrypt
                .HashPassword(password, workFactor: 11);

            // Initialize new User object
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Step 1: Save User to Database
            await _userRepo.AddAsync(user);

            // Step 2: Create a default Portfolio for the new user
            var portfolio = new Portfolio
            {
                UserId = user.Id,
                Name = username + "'s Portfolio",
                Wallet = new Wallet(10000m), // Starting balance
                CreatedAt = DateTime.UtcNow
            };

            // Step 3: Save Portfolio to Database
            await _portfolioRepo.AddAsync(portfolio);

            return Tuple.Create(true,
                "Account created successfully! Your wallet has been credited with $10,000.",
                user);
        }

        // Authenticates an existing user
        public async Task<Tuple<bool, string, User>> LoginAsync(
            string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
                return Tuple.Create(false,
                    "Username and password are required",
                    (User)null);

            // Retrieve user from the repository
            var user = await _userRepo.GetByUsernameAsync(username);
            if (user == null)
                return Tuple.Create(false,
                    "Invalid username or password",
                    (User)null);

            // Verify the provided password against the stored hash
            bool isValid = BCrypt.Net.BCrypt
                .Verify(password, user.PasswordHash);

            if (!isValid)
                return Tuple.Create(false,
                    "Invalid username or password",
                    (User)null);

            return Tuple.Create(true,
                "Welcome back, " + username + "!",
                user);
        }
    }
}