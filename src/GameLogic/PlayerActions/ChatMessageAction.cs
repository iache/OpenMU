﻿// <copyright file="ChatMessageAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions;

using System.ComponentModel;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views;

/// <summary>
/// Action to send chat messages.
/// </summary>
public class ChatMessageAction
{
    private readonly IDictionary<string, ChatMessageType> _messagePrefixes;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatMessageAction"/> class.
    /// </summary>
    public ChatMessageAction()
    {
        this._messagePrefixes = new SortedDictionary<string, ChatMessageType>(new ReverseComparer())
        {
            { "~", ChatMessageType.Party },
            { "@", ChatMessageType.Guild },
            { "@@", ChatMessageType.Alliance },
            { "$", ChatMessageType.Gens },
            { "!", ChatMessageType.GlobalNotification },
            { "/", ChatMessageType.Command },
        };
    }

    /// <summary>
    /// Sends a chat message from the player to other players.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="playerName">Name of the <paramref name="sender"/>, except for <see cref="ChatMessageType.Whisper"/>, then its the receiver name.</param>
    /// <param name="message">The message which should be sent.</param>
    /// <param name="whisper">If set to <c>true</c> the message is whispered to the player with the <paramref name="playerName"/>; Otherwise, it's not a whisper.</param>
    public async ValueTask ChatMessageAsync(Player sender, string playerName, string message, bool whisper)
    {
        using var loggerScope = sender.Logger.BeginScope(this.GetType());
        ChatMessageType messageType = this.GetMessageType(message, whisper);
        if (messageType != ChatMessageType.Whisper && playerName != sender.SelectedCharacter?.Name)
        {
            sender.Logger.LogWarning("Maybe Hacker, Charname in chat packet != charname\t [{0}] <> [{1}]", sender.SelectedCharacter?.Name, playerName);
        }

        switch (messageType)
        {
            case ChatMessageType.Command:
                var commandKey = message.Split(' ').First();
                var commandHandler = sender.GameContext.PlugInManager.GetStrategy<IChatCommandPlugIn>(commandKey);
                if (commandHandler is null)
                {
                    break;
                }

                if (sender.SelectedCharacter!.CharacterStatus >= commandHandler.MinCharacterStatusRequirement)
                {
                    await commandHandler.HandleCommandAsync(sender, message).ConfigureAwait(false);
                }
                else
                {
                    sender.Logger.LogWarning($"{sender.Name} is trying to execute {commandKey} command without meeting the requirements");
                }

                break;
            case ChatMessageType.Whisper:
                var whisperReceiver = sender.GameContext.GetPlayerByCharacterName(playerName);
                if (whisperReceiver != null)
                {
                    var eventArgs = new CancelEventArgs();
                    sender.GameContext.PlugInManager.GetPlugInPoint<IWhisperMessageReceivedPlugIn>()?.WhisperMessageReceived(sender, whisperReceiver, message, eventArgs);
                    if (!eventArgs.Cancel)
                    {
                        await whisperReceiver.InvokeViewPlugInAsync<IChatViewPlugIn>(p => p.ChatMessageAsync(message, sender.SelectedCharacter!.Name, ChatMessageType.Whisper)).ConfigureAwait(false);
                    }
                }

                break;
            default:
                await this.HandleChatMessageAsync(sender, message, messageType).ConfigureAwait(false);
                break;
        }
    }

    private async ValueTask HandleChatMessageAsync(Player sender, string message, ChatMessageType messageType)
    {
        sender.Logger.LogDebug("Chat Message Received: [{0}]:[{1}]", sender.SelectedCharacter!.Name, message);
        var eventArgs = new CancelEventArgs();
        sender.GameContext.PlugInManager.GetPlugInPoint<IChatMessageReceivedPlugIn>()?.ChatMessageReceived(sender, message, eventArgs);
        if (eventArgs.Cancel)
        {
            return;
        }

        switch (messageType)
        {
            case ChatMessageType.Party:
                sender.Party?.SendChatMessageAsync(message, sender.SelectedCharacter.Name);
                break;
            case ChatMessageType.Alliance:
            {
                if (sender.GuildStatus != null
                    && (sender.GameContext as IGameServerContext)?.EventPublisher is { } publisher)
                {
                    // TODO: Use DI to get the IEventPublisher
                    await publisher.AllianceMessageAsync(sender.GuildStatus.GuildId, sender.SelectedCharacter.Name, message).ConfigureAwait(false);
                }

                break;
            }

            case ChatMessageType.Guild:
            {
                if (sender.GuildStatus != null
                    && (sender.GameContext as IGameServerContext)?.EventPublisher is { } publisher)
                {
                    await publisher.GuildMessageAsync(sender.GuildStatus.GuildId, sender.SelectedCharacter.Name, message).ConfigureAwait(false);
                }

                break;
            }

            case ChatMessageType.GlobalNotification:
            {
                if (sender.SelectedCharacter.CharacterStatus >= CharacterStatus.GameMaster)
                {
                    await sender.GameContext.SendGlobalNotificationAsync(message).ConfigureAwait(false);
                }

                break;
            }

            default:
                sender.Logger.LogDebug("Sending Chat Message to Observers, Count: {0}", sender.Observers.Count);
                await sender.ForEachWorldObserverAsync<IChatViewPlugIn>(p => p.ChatMessageAsync(message, sender.SelectedCharacter.Name, ChatMessageType.Normal), true).ConfigureAwait(false);
                break;
        }
    }

    private ChatMessageType GetMessageType(string message, bool whisper)
    {
        if (whisper)
        {
            return ChatMessageType.Whisper;
        }

        // byte 13: begin message
        foreach (var keyValuePair in this._messagePrefixes)
        {
            if (message.StartsWith(keyValuePair.Key, StringComparison.InvariantCulture))
            {
                return keyValuePair.Value;
            }
        }

        return ChatMessageType.Normal;
    }

    /// <summary>
    /// We have to implement a reverse comparer, so that the strings which are longer, come first.
    /// </summary>
    private class ReverseComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            return string.Compare(y, x, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}