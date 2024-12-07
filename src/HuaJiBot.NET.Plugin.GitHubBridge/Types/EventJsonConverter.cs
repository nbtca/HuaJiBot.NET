#nullable disable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HuaJiBot.NET.Plugin.GitHubBridge.Types;

internal class EventJsonConverter : JsonConverter<Event>
{
    public override void WriteJson(JsonWriter writer, Event value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("headers");
        serializer.Serialize(writer, value.Headers);
        writer.WritePropertyName("body");
        if (value.Body is UnknownEventBody unknown)
            serializer.Serialize(writer, unknown.Raw);
        else
            serializer.Serialize(writer, value.Body);
        writer.WriteEndObject();
    }

    public override Event ReadJson(
        JsonReader reader,
        Type objectType,
        Event existingValue,
        bool hasExistingValue,
        JsonSerializer serializer
    )
    {
        var obj = JObject.Load(reader);
        var headers = obj["headers"]!.ToObject<Headers>(serializer)!;
        var bodyJsonObj = obj["body"]!;
        return new Event
        {
            Headers = headers,
            Body = headers.XGithubEvent switch
            {
                [
                    "push",
                ] //仓库提交事件（commit）
                => bodyJsonObj.ToObject<PushEventBody.PushEventBody>(serializer),
                [
                    "workflow_run",
                ] //Workflow运行事件
                => bodyJsonObj.ToObject<WorkflowRunEventBody.WorkflowRunEventBody>(serializer),
                [
                    "issues",
                ] //issue变更
                => bodyJsonObj.ToObject<IssuesEventBody.IssuesEventBody>(serializer),
                _ => new UnknownEventBody
                {
                    Raw = bodyJsonObj,
                } //未知事件
                ,
            },
        };
    }
}
