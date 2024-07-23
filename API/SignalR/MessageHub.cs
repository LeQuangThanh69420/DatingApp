using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        public MessageHub(IMessageRepository messageRepository, IMapper mapper, IUserRepository userRepository)
        {
            _messageRepository = messageRepository;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(Context.User.GetUsername(), otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var messages = await _messageRepository.GetMessageThread(Context.User.GetUsername(), otherUser);

            await Clients.Groups(groupName).SendAsync("ReceiveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDTO createMessageDTO)
        {
            var username = Context.User.GetUsername();
            if(username == createMessageDTO.RecipientUsername.ToLower()) {
                throw new HubException("You cannot send messages to yourself!");
            }

            var sender = await _userRepository.GetUserByUsernameAsync(username);
            var recipient = await _userRepository.GetUserByUsernameAsync(createMessageDTO.RecipientUsername);
            
            if(recipient == null) {
                throw new HubException("Not found user");
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUsername = sender.UserName,
                RecipientUsername = recipient.UserName,
                Content = createMessageDTO.Content,
            };
            _messageRepository.AddMessage(message);

            if(await _messageRepository.SaveAllAsync()) {
                var groupName = GetGroupName(sender.UserName, recipient.UserName);
                await Clients.Groups(groupName).SendAsync("NewMessage", _mapper.Map<MessageDTO>(message));
            }
        }

        private string GetGroupName(string caller, string? other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}