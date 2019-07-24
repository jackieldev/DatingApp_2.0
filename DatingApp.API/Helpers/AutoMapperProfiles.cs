using System.Linq;
using AutoMapper;
using DatingApp.API.Dtos;
using DatingApp.API.Models;

namespace DatingApp.API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            this.CreateMap<User, UserForListDto>()
            .ForMember(dest => dest.PhotoUrl, opt =>
            {
                opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
            }).ForMember(dest => dest.Age, opt =>
            {
                opt.MapFrom(d => d.DateOfBirth.CalculateAge());
            });

            this.CreateMap<User, UserForDetailedDto>()
            .ForMember(dest => dest.PhotoUrl, opt =>
            {
                opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
            }).ForMember(dest => dest.Age, opt =>
            {
                opt.MapFrom(d => d.DateOfBirth.CalculateAge());
            });

            this.CreateMap<Photo, PhotosForDetailedDto>();
            this.CreateMap<UserForUpdateDto, User>();
            this.CreateMap<Photo, PhotoForReturnDto>();
            this.CreateMap<PhotoForCreationDto, Photo>();
            this.CreateMap<UserForRegisterDto, User>();
            this.CreateMap<MessageForCreationDto, Message>().ReverseMap();
            this.CreateMap<Message, MessageToReturnDto>()
                .ForMember(a => a.SenderPhotoUrl, opt => opt
                    .MapFrom(a => a.Sender.Photos.FirstOrDefault(p => p.IsMain).Url))
                .ForMember(a => a.RecipientPhotoUrl, opt => opt
                    .MapFrom(a => a.Recipient.Photos.FirstOrDefault(p => p.IsMain).Url));
        }
    }
}