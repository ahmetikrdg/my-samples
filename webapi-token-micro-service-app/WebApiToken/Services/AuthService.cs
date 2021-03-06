﻿using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebApiToken.Models;

namespace WebApiToken.Services
{
    public class AuthService : IAuthService
    {
        private DatabaseContext _context;

        public AuthService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> IsUserExists(string username, string appname)
        {
            return await _context.Users.AnyAsync(u =>
                u.Username.ToLower() == username && u.AppName.ToLower() == appname.ToLower());
        }

        public async Task<User> Login(string username, string password, string appname)
        {
            User user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Username.ToLower() == username.ToLower() && u.AppName.ToLower() == appname.ToLower());

            if (user == null) return null;
            if (!VerifyPassword(password, user.PasswordSalt, user.PasswordHash)) return null;

            return user;
        }

        private bool VerifyPassword(string password, byte[] passwordSalt, byte[] passwordHash)
        {
            using (HMACSHA512 hmac = new HMACSHA512(passwordSalt))
            {
                byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

                if (computedHash.SequenceEqual(passwordHash) == false) return false;

                return true;
            }
        }

        public async Task<User> Register(User user, string password, string appname)
        {
            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(password, out passwordHash, out passwordSalt);

            user.PasswordSalt = passwordSalt;
            user.PasswordHash = passwordHash;
            user.AppName = appname;

            await _context.Users.AddAsync(user);
            if (await _context.SaveChangesAsync() == 0)
            {
                return null;
            }

            return user;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (HMACSHA512 hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
