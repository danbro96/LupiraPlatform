using System.Text.Json.Serialization;

namespace Lupira.Contracts.Dav;

[JsonConverter(typeof(JsonStringEnumConverter<DavCollectionKind>))]
public enum DavCollectionKind
{
    EventCalendar,
    TodoList,
    AddressBook,
}
