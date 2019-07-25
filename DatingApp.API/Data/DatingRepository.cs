using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Data
{
    public class DatingRepository : IDatingRepository
    {
        private readonly DataContext _context;

        public DatingRepository(DataContext context)
        {
            this._context = context;
        }
        public void Add<T>(T entity) where T : class
        {
            this._context.Add(entity);
        }

        public void Delete<T>(T entity) where T : class
        {
            this._context.Remove(entity);
        }

        public async Task<Like> GetLike(int userId, int recipientId)
        {
            return await this._context.Likes
                .FirstOrDefaultAsync(a => a.LikerId == userId && a.LikeeId == recipientId);
        }

        public async Task<Photo> GetMainPhotoForUser(int userId)
        {
            return await this._context.Photos.Where(p => p.UserId == userId)
                .FirstOrDefaultAsync(p => p.IsMain);
        }

        public async Task<Photo> GetPhoto(int id)
        {
            return await this._context.Photos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id.Equals(id));
        }

        public async Task<User> GetUser(int id, bool isCurrentUser)
        {
            var query = this._context.Users.Include(p => p.Photos).AsQueryable();

            if (isCurrentUser)
                query = query.IgnoreQueryFilters();

            return await query.FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<PagedList<User>> GetUsers(UserParams userParams)
        {
            var users = this._context.Users.Include(a => a.Photos)
                .OrderByDescending(a => a.LastActive).AsQueryable();

            users = users.Where(a => a.Id != userParams.UserId);

            users = users.Where(a => a.Gender == userParams.Gender);

            if (userParams.Likers)
            {
                var usersLikers = await this.GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => usersLikers.Contains(u.Id));
            }

            if (userParams.Likees)
            {
                var usersLikees = await this.GetUserLikes(userParams.UserId, userParams.Likers);
                users = users.Where(u => usersLikees.Contains(u.Id));
            }

            if (userParams.MinAge != 18 || userParams.MaxAge != 99)
            {
                var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
                var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

                users = users.Where(a => a.DateOfBirth >= minDob && a.DateOfBirth <= maxDob);
            }

            if (!string.IsNullOrEmpty(userParams.OrderBy))
            {
                switch (userParams.OrderBy)
                {
                    case "created":
                        users = users.OrderByDescending(a => a.Created);
                        break;
                    default:
                        users = users.OrderByDescending(a => a.LastActive);
                        break;
                }
            }

            return await PagedList<User>.CreateAsync(users, userParams.PageNumber, userParams.PageSize);
        }

        private async Task<IEnumerable<int>> GetUserLikes(int id, bool likers)
        {
            var user = await this._context.Users
                .Include(a => a.Likers)
                .Include(a => a.Likees)
                .FirstOrDefaultAsync(a => a.Id == id);

            return likers ? user.Likers.Where(a => a.LikeeId == id).Select(i => i.LikerId)
                          : user.Likees.Where(a => a.LikerId == id).Select(i => i.LikeeId);
        }

        public async Task<bool> SaveAll()
        {
            return await this._context.SaveChangesAsync() > 0;
        }

        public async Task<Message> GetMessage(int id)
        {
            return await this._context.Messages.FirstOrDefaultAsync(a => a.Id.Equals(id));
        }

        public async Task<PagedList<Message>> GetMessagesForUser(MessageParams messageParams)
        {
            var messages = this._context.Messages
                .Include(a => a.Sender).ThenInclude(p => p.Photos)
                .Include(a => a.Recipient).ThenInclude(p => p.Photos)
                .AsQueryable();

            switch (messageParams.MessageContainer)
            {
                case "Inbox":
                    messages = messages.Where(a => a.RecipientId == messageParams.UserId
                        && !a.RecipientDeleted);
                    break;
                case "Outbox":
                    messages = messages.Where(a => a.SenderId == messageParams.UserId
                        && !a.SenderDeleted);
                    break;
                default:
                    messages = messages.Where(a => a.RecipientId == messageParams.UserId
                        && !a.RecipientDeleted && !a.IsRead);
                    break;
            }

            messages = messages.OrderByDescending(a => a.MessageSend);

            return await PagedList<Message>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<Message>> GetMessageThread(int userId, int recipientId)
        {
            var messages = await this._context.Messages
               .Include(a => a.Sender).ThenInclude(p => p.Photos)
               .Include(a => a.Recipient).ThenInclude(p => p.Photos)
               .Where(a => a.RecipientId == userId
                        && !a.RecipientDeleted
                        && a.SenderId == recipientId
                        || a.RecipientId == recipientId
                        && a.SenderId == userId
                        && !a.SenderDeleted)
               .OrderByDescending(o => o.MessageSend)
               .ToListAsync();

            return messages;
        }
    }
}