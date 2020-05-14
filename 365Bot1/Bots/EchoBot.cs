// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.6.2

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using System.Linq;
using Microsoft.Bot.Builder.AI.QnA;
using Remotion.Linq.Parsing.ExpressionVisitors.Transformation.PredefinedTransformations;
using System.Security.AccessControl;
using Microsoft.Extensions.Logging;

namespace _365Bot1.Bots
{


    public class EchoBot : ActivityHandler
    {

        // Create local Memory Storage.
        private static readonly MemoryStorage _myStorage = new MemoryStorage();

        // Create cancellation token (used by Async Write operation).
        public CancellationToken cancellationToken { get; private set; }

        // Class for storing a log of utterances (text of messages) as a list.
        public class UtteranceLog : IStoreItem
        {
            // A list of things that users have said to the bot
            public List<string> UtteranceList { get; } = new List<string>();

            // The number of conversational turns that have occurred
            public int TurnNumber { get; set; } = 0;

            // Create concurrency control where this is used.
            public string ETag { get; set; } = "*";
        }

        public QnAMaker EchoBotQnA { get; private set; }
        public EchoBot(QnAMakerEndpoint endpoint)
        {
            //connects the QnA Maker endpoint for each turn
            EchoBotQnA = new QnAMaker(endpoint);
        }

        //this echoes every message back to the user
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var utterance = turnContext.Activity.Text;
            UtteranceLog logItems = null;

            //check if there are already messgaes in the list messages
            try
            {
                string[] utteranceList = { "UtteranceLog" };
                logItems = _myStorage.ReadAsync<UtteranceLog>(utteranceList).Result?.FirstOrDefault().Value;
                int? check = _myStorage.ReadAsync<UtteranceLog>(utteranceList).Result?.Count();
            }
            catch
            {
                // Inform the user an error occured.
                await turnContext.SendActivityAsync("Sorry, something went wrong reading your stored messages!");

            }

            //if theres nothing in te list then create an uttrnace log and add the first item
            if (logItems is null)
            {
                //add the message to the list
                logItems = new UtteranceLog();
                logItems.UtteranceList.Add(utterance);
                logItems.TurnNumber++;

                //Show the user the number of turns theyve had
                await turnContext.SendActivityAsync($"{logItems.TurnNumber}: The list is now: {string.Join(", ", logItems.UtteranceList)}");
                

                //create dictionary to hold messages
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                }
                try
                //save the message to storage
                {
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    await turnContext.SendActivityAsync("Sorry, something dun fucked up");
                }
            }
            else
            //if the storage already contained other messages, so add another one
            {
                logItems.UtteranceList.Add(utterance);
                logItems.TurnNumber++;

                // show user new list of saved messages.
                await turnContext.SendActivityAsync($"{logItems.TurnNumber}: The list is now: {string.Join(", ", logItems.UtteranceList)}");
                

                // Create Dictionary object to hold new list of messages.
                var changes = new Dictionary<string, object>();
                {
                    changes.Add("UtteranceLog", logItems);
                };

                try
                {
                    // Save new list to your Storage.
                    await _myStorage.WriteAsync(changes, cancellationToken);
                }
                catch
                {
                    // Inform the user an error occured.
                    await turnContext.SendActivityAsync("Sorry, something went wrong storing your message!");
                }
               
            }
          








            //old echo bot coe
            //var replyText = $"Echo: {turnContext.Activity.Text}";
            //await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);


            //await AccessQnAMaker(turnContext, cancellationToken);
        }

        //this is called when a new user is added and checks to see if they if they exist in the coversation, if not then it says hello!
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Hello and welcome!";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        //this connects to qna service
        private async Task AccessQnAMaker(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var results = await EchoBotQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("QnA Maker Returned: " + results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A system."), cancellationToken);
            }
        }
    }
}
