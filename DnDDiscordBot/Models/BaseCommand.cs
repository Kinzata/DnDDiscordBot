using System;
using System.Threading.Tasks;

namespace DnDDiscordBot.Models
{
    public abstract class BaseCommand
    {
        public BaseCommand(IServiceProvider services)
        {

        }

        public abstract Task Execute(object commandArgs, DndActionContext actionContext);
    }
}
