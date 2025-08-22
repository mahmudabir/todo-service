using AutoMapper;

using Domain.Entities.Todos;

using Shared.Models.Todos;

namespace Application.Mappers;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        CreateMap<Todo, TodoViewModel>()
            .ReverseMap()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore());
    }
}