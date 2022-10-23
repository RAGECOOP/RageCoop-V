﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GTA.Native;
using Lidgren.Network;
using RageCoop.Core;
using RageCoop.Core.Scripting;

namespace RageCoop.Server.Scripting;

/// <summary>
/// </summary>
public class ServerEvents
{
    private readonly Server Server;

    #region INTERNAL

    internal Dictionary<int, List<Action<CustomEventReceivedArgs>>> CustomEventHandlers = new();

    #endregion

    internal ServerEvents(Server server)
    {
        Server = server;
    }

    /// <summary>
    ///     Invoked when a chat message is received.
    /// </summary>
    public event EventHandler<ChatEventArgs> OnChatMessage;

    /// <summary>
    ///     Will be invoked from main thread before registered handlers
    /// </summary>
    public event EventHandler<OnCommandEventArgs> OnCommandReceived;

    /// <summary>
    ///     Will be invoked from main thread when a client is attempting to connect, use
    ///     <see cref="HandshakeEventArgs.Deny(string)" /> to deny the connection request.
    /// </summary>
    public event EventHandler<HandshakeEventArgs> OnPlayerHandshake;

    /// <summary>
    ///     Will be invoked when a player is connected, but this player might not be ready yet(client resources not loaded),
    ///     using <see cref="OnPlayerReady" /> is recommended.
    /// </summary>
    public event EventHandler<Client> OnPlayerConnected;

    /// <summary>
    ///     Will be invoked after the client connected and all resources(if any) have been loaded.
    /// </summary>
    public event EventHandler<Client> OnPlayerReady;

    /// <summary>
    ///     Invoked when a player disconnected, all method won't be effective in this scope.
    /// </summary>
    public event EventHandler<Client> OnPlayerDisconnected;

    /// <summary>
    ///     Invoked everytime a player's main ped has been updated
    /// </summary>
    public event EventHandler<Client> OnPlayerUpdate;

    internal void ClearHandlers()
    {
        OnChatMessage = null;
        OnPlayerHandshake = null;
        OnPlayerConnected = null;
        OnPlayerReady = null;
        OnPlayerDisconnected = null;
        // OnCustomEventReceived=null;
        OnCommandReceived = null;
        OnPlayerUpdate = null;
    }

    #region INVOKE

    internal void InvokePlayerHandshake(HandshakeEventArgs args)
    {
        OnPlayerHandshake?.Invoke(this, args);
    }

    internal void InvokeOnCommandReceived(string cmdName, string[] cmdArgs, Client sender)
    {
        var args = new OnCommandEventArgs
        {
            Name = cmdName,
            Args = cmdArgs,
            Client = sender
        };
        OnCommandReceived?.Invoke(this, args);
        if (args.Cancel) return;
        if (Server.Commands.Any(x => x.Key.Name == cmdName))
        {
            var argsWithoutCmd = cmdArgs.Skip(1).ToArray();

            CommandContext ctx = new()
            {
                Client = sender,
                Args = argsWithoutCmd
            };

            var command = Server.Commands.First(x => x.Key.Name == cmdName);
            command.Value.Invoke(ctx);
        }
        else
        {
            Server.SendChatMessage("Server", "Command not found!", sender);
        }
    }

    internal void InvokeOnChatMessage(string msg, Client sender, string clamiedSender = null)
    {
        OnChatMessage?.Invoke(this, new ChatEventArgs
        {
            Client = sender,
            Message = msg,
            ClaimedSender = clamiedSender
        });
    }

    internal void InvokePlayerConnected(Client client)
    {
        OnPlayerConnected?.Invoke(this, client);
    }

    internal void InvokePlayerReady(Client client)
    {
        OnPlayerReady?.Invoke(this, client);
    }

    internal void InvokePlayerDisconnected(Client client)
    {
        OnPlayerDisconnected?.Invoke(this, client);
    }

    internal void InvokeCustomEventReceived(Packets.CustomEvent p, Client sender)
    {
        var args = new CustomEventReceivedArgs { Hash = p.Hash, Args = p.Args, Client = sender };
        if (CustomEventHandlers.TryGetValue(p.Hash, out var handlers)) handlers.ForEach(x => { x.Invoke(args); });
    }

    internal void InvokePlayerUpdate(Client client)
    {
        OnPlayerUpdate?.Invoke(this, client);
    }

    #endregion
}

/// <summary>
///     An class that can be used to interact with RageCoop server.
/// </summary>
public class API
{
    /// <summary>
    ///     Server side events
    /// </summary>
    public readonly ServerEvents Events;

    internal readonly Dictionary<string, Func<Stream>> RegisteredFiles = new();
    internal readonly Server Server;

    internal API(Server server)
    {
        Server = server;
        Events = new ServerEvents(server);
        Server.RequestHandlers.Add(PacketType.FileTransferRequest, (data, client) =>
        {
            var p = new Packets.FileTransferRequest();
            p.Deserialize(data);
            var id = Server.NewFileID();
            if (RegisteredFiles.TryGetValue(p.Name, out var s))
            {
                Task.Run(() => { Server.SendFile(s(), p.Name, client, id); });
                return new Packets.FileTransferResponse
                {
                    ID = id,
                    Response = FileResponse.Loaded
                };
            }

            return new Packets.FileTransferResponse
            {
                ID = id,
                Response = FileResponse.LoadFailed
            };
        });
    }

    /// <summary>
    ///     All synchronized entities on this server.
    /// </summary>
    public ServerEntities Entities => Server.Entities;

    #region FUNCTIONS

    /// <summary>
    ///     Get a list of all Clients
    /// </summary>
    /// <returns>All clients as a dictionary indexed by their main character's id</returns>
    public Dictionary<int, Client> GetAllClients()
    {
        return new Dictionary<int, Client>(Server.ClientsByID);
    }

    /// <summary>
    ///     Get the client by its username
    /// </summary>
    /// <param name="username">The username to search for (non case-sensitive)</param>
    /// <returns>The Client from this user or null</returns>
    public Client GetClientByUsername(string username)
    {
        Server.ClientsByName.TryGetValue(username, out var c);
        return c;
    }

    /// <summary>
    ///     Send a chat message to all players, use <see cref="Client.SendChatMessage(string, string)" /> to send to an
    ///     individual client.
    /// </summary>
    /// <param name="targets">The clients to send message, leave it null to send to all clients</param>
    /// <param name="message">The chat message</param>
    /// <param name="username">The username which send this message (default = "Server")</param>
    /// <param name="raiseEvent">
    ///     Weather to raise the <see cref="ServerEvents.OnChatMessage" /> event defined in
    ///     <see cref="API.Events" />
    /// </param>
    /// <remarks>
    ///     When <paramref name="raiseEvent" /> is unspecified and <paramref name="targets" /> is null or unspecified,
    ///     <paramref name="raiseEvent" /> will be set to true
    /// </remarks>
    public void SendChatMessage(string message, List<Client> targets = null, string username = "Server",
        bool? raiseEvent = null)
    {
        raiseEvent ??= targets == null;
        try
        {
            if (Server.MainNetServer.ConnectionsCount != 0)
            {
                targets ??= new List<Client>(Server.ClientsByNetHandle.Values);
                foreach (var client in targets) Server.SendChatMessage(username, message, client);
            }
        }
        catch (Exception e)
        {
            Server.Logger?.Error(
                $">> {e.Message} <<>> {e.Source ?? string.Empty} <<>> {e.StackTrace ?? string.Empty} <<");
        }

        if (raiseEvent.Value) Events.InvokeOnChatMessage(message, null, username);
    }

    /// <summary>
    ///     Register a file to be shared with clients
    /// </summary>
    /// <param name="name">name of this file</param>
    /// <param name="path">path to this file</param>
    public void RegisterSharedFile(string name, string path)
    {
        RegisteredFiles.Add(name, () => { return File.OpenRead(path); });
    }

    /// <summary>
    ///     Register a file to be shared with clients
    /// </summary>
    /// <param name="name">name of this file</param>
    /// <param name="file"></param>
    public void RegisterSharedFile(string name, ResourceFile file)
    {
        RegisteredFiles.Add(name, file.GetStream);
    }

    /// <summary>
    ///     Register a new command chat command (Example: "/test")
    /// </summary>
    /// <param name="name">The name of the command (Example: "test" for "/test")</param>
    /// <param name="usage">How to use this message (argsLength required!)</param>
    /// <param name="argsLength">The length of args (Example: "/message USERNAME MESSAGE" = 2) (usage required!)</param>
    /// <param name="callback">A callback to invoke when the command received.</param>
    public void RegisterCommand(string name, string usage, short argsLength, Action<CommandContext> callback)
    {
        Server.RegisterCommand(name, usage, argsLength, callback);
    }

    /// <summary>
    ///     Register a new command chat command (Example: "/test")
    /// </summary>
    /// <param name="name">The name of the command (Example: "test" for "/test")</param>
    /// <param name="callback">A callback to invoke when the command received.</param>
    public void RegisterCommand(string name, Action<CommandContext> callback)
    {
        Server.RegisterCommand(name, callback);
    }

    /// <summary>
    ///     Register all commands in a static class
    /// </summary>
    /// <typeparam name="T">Your static class with commands</typeparam>
    public void RegisterCommands<T>()
    {
        Server.RegisterCommands<T>();
    }

    /// <summary>
    ///     Register all commands inside an class instance
    /// </summary>
    /// <param name="obj">The instance of type containing the commands</param>
    public void RegisterCommands(object obj)
    {
        var commands = obj.GetType().GetMethods()
            .Where(method => method.GetCustomAttributes(typeof(Command), false).Any());

        foreach (var method in commands)
        {
            var attribute = method.GetCustomAttribute<Command>(true);
            RegisterCommand(attribute.Name, attribute.Usage, attribute.ArgsLength,
                ctx => { method.Invoke(obj, new object[] { ctx }); });
        }
    }

    /// <summary>
    ///     Send native call specified clients.
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="args"></param>
    /// ///
    /// <param name="clients">Clients to send, null for all clients</param>
    public void SendNativeCall(List<Client> clients, Hash hash, params object[] args)
    {
        var argsList = new List<object>(args);
        argsList.InsertRange(0, new object[] { (byte)TypeCode.Empty, (ulong)hash });
        SendCustomEvent(CustomEventFlags.Queued, clients, CustomEvents.NativeCall, argsList.ToArray());
    }


    /// <summary>
    ///     Send an event and data to the specified clients.
    /// </summary>
    /// <param name="flags"></param>
    /// <param name="eventHash">An unique identifier of the event/> to get it from a string</param>
    /// <param name="args">
    ///     The objects conataing your data, see <see cref="Scripting.CustomEventReceivedArgs.Args" /> for
    ///     supported types.
    /// </param>
    /// <param name="targets">The target clients to send. Leave it null to send to all clients</param>
    public void SendCustomEvent(CustomEventFlags flags, List<Client> targets, CustomEventHash eventHash,
        params object[] args)
    {
        var p = new Packets.CustomEvent(flags)
        {
            Args = args,
            Hash = eventHash
        };
        if (targets == null)
            Server.SendToAll(p, ConnectionChannel.Event, NetDeliveryMethod.ReliableOrdered);
        else
            foreach (var c in targets)
                Server.Send(p, c, ConnectionChannel.Event, NetDeliveryMethod.ReliableOrdered);
    }

    public void SendCustomEvent(List<Client> targets, CustomEventHash eventHash, params object[] args)
    {
        SendCustomEvent(CustomEventFlags.None, targets, eventHash, args);
    }

    public void SendCustomEventQueued(List<Client> targets, CustomEventHash eventHash, params object[] args)
    {
        SendCustomEvent(CustomEventFlags.Queued, targets, eventHash, args);
    }

    /// <summary>
    ///     Register an handler to the specifed event hash, one event can have multiple handlers.
    /// </summary>
    /// <param name="hash">An unique identifier of the event></param>
    /// <param name="handler">An handler to be invoked when the event is received from the server.</param>
    public void RegisterCustomEventHandler(CustomEventHash hash, Action<CustomEventReceivedArgs> handler)
    {
        lock (Events.CustomEventHandlers)
        {
            if (!Events.CustomEventHandlers.TryGetValue(hash, out var handlers))
                Events.CustomEventHandlers.Add(hash, handlers = new List<Action<CustomEventReceivedArgs>>());
            handlers.Add(handler);
        }
    }


    /// <summary>
    ///     Find a script matching the specified type
    /// </summary>
    /// <param name="scriptFullName">The full name of the script's type, e.g. RageCoop.Resources.Discord.Main</param>
    /// <param name="resourceName">Which resource to search for this script. Will search in all loaded resources if unspecified </param>
    /// <returns>A <see langword="dynamic" /> object reprensenting the script, or <see langword="null" /> if not found.</returns>
    /// <remarks>
    ///     Explicitly casting the return value to orginal type will case a exception to be thrown due to the dependency
    ///     isolation mechanism in resource system.
    ///     You shouldn't reference the target resource assemblies either, since it causes the referenced assembly to be loaded
    ///     and started in your resource.
    /// </remarks>
    public dynamic FindScript(string scriptFullName, string resourceName = null)
    {
        if (resourceName == null)
        {
            foreach (var res in LoadedResources.Values)
                if (res.Scripts.TryGetValue(scriptFullName, out var script))
                    return script;
        }
        else if (LoadedResources.TryGetValue(resourceName, out var res))
        {
            if (res.Scripts.TryGetValue(scriptFullName, out var script)) return script;
        }

        return null;
    }

    #endregion


    #region PROPERTIES

    /// <summary>
    ///     Get a <see cref="Core.Logger" /> that the server is currently using, you should use
    ///     <see cref="ServerResource.Logger" /> to display resource-specific information.
    /// </summary>
    public Logger Logger => Server.Logger;


    /// <summary>
    ///     Gets or sets the client that is resposible for synchronizing time and weather
    /// </summary>
    public Client Host
    {
        get => Server._hostClient;
        set
        {
            if (Server._hostClient != value)
            {
                Server._hostClient?.SendCustomEvent(CustomEvents.IsHost, false);
                value.SendCustomEvent(CustomEvents.IsHost, true);
                Server._hostClient = value;
            }
        }
    }

    /// <summary>
    ///     Get all currently loaded <see cref="ServerResource" /> as a dictionary indexed by their names
    /// </summary>
    /// <remarks>
    ///     Accessing this property from script constructor is stronly discouraged since other scripts and resources might have
    ///     yet been loaded.
    ///     Accessing from <see cref="ServerScript.OnStart" /> is not recommended either. Although all script assemblies will
    ///     have been loaded to memory and instantiated, <see cref="ServerScript.OnStart" /> invocation of other scripts are
    ///     not guaranteed.
    /// </remarks>
    public Dictionary<string, ServerResource> LoadedResources
    {
        get
        {
            if (!Server.Resources.IsLoaded)
            {
                Logger?.Warning("Attempting to get resources before all scripts are loaded");
                Logger.Trace(new StackTrace().ToString());
            }

            return Server.Resources.LoadedResources;
        }
    }

    #endregion
}