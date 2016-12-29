﻿/*
This source file is subject to version 3 of the GPL license,
that is bundled with this package in the file LICENSE, and is
available online at http://www.gnu.org/licenses/gpl.txt;
you may not use this file except in compliance with the License.

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

All portions of the code written by Voat are Copyright (c) 2015 Voat, Inc.
All Rights Reserved.
*/

using System;
using System.Threading.Tasks;
using Voat.Caching;
using Voat.Data;

//using Microsoft.AspNet.SignalR;
//using Voat.Data.Models;
using Voat.Data.Models;
using Voat.Domain.Query;

namespace Voat.Utilities.Components
{
    public static class NotificationManager
    {
        public static async Task SendUserMentionNotification(string user, Comment comment)
        {
            if (comment != null)
            {
                if (!UserHelper.UserExists(user))
                {
                    return;
                }
                try
                {
                    string recipient = UserHelper.OriginalUsername(user);

                    //BlockedUser Implementation - Comment User Mention
                    if (!MesssagingUtility.IsSenderBlocked(comment.UserName, recipient))
                    {
                        //var submission = DataCache.Submission.Retrieve(comment.SubmissionID);

                        var q = new QuerySubmission(comment.SubmissionID.Value);
                        var submission = await q.ExecuteAsync().ConfigureAwait(false);
                        //var subverse = DataCache.Subverse.Retrieve(submission.Subverse);

                        var message = new Domain.Models.Message();

                        message.IsAnonymized = comment.IsAnonymized;
                        message.Recipient = recipient;
                        message.RecipientType = Domain.Models.IdentityType.User;
                        message.Sender = comment.UserName;
                        message.SenderType = Domain.Models.IdentityType.User;
                        message.Subverse = submission.Subverse;
                        message.SubmissionID = comment.SubmissionID;
                        message.CommentID = comment.ID;
                        message.Type = Domain.Models.MessageType.CommentMention;
                        message.CreationDate = Repository.CurrentDate;

                        using (var repo = new Repository())
                        {
                            var response = await repo.SendMessage(message);

                            if (response.Success)
                            {
                                EventNotification.Instance.SendMentionNotice(recipient, comment.UserName, Domain.Models.ContentType.Comment, comment.ID, comment.Content);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static async Task SendUserMentionNotification(string user, Submission submission)
        {
            if (submission != null)
            {
                if (!UserHelper.UserExists(user))
                {
                    return;
                }
                try
                {
                    string recipient = UserHelper.OriginalUsername(user);

                    //BlockedUser Implementation - Submission User Mention
                    if (!MesssagingUtility.IsSenderBlocked(submission.UserName, recipient))
                    {
                        //var subverse = DataCache.Subverse.Retrieve(submission.Subverse);
                        var message = new Domain.Models.Message();

                        message.IsAnonymized = submission.IsAnonymized;
                        message.Recipient = recipient;
                        message.RecipientType = Domain.Models.IdentityType.User;
                        message.Sender = submission.UserName;
                        message.SenderType = Domain.Models.IdentityType.User;
                        message.Subverse = submission.Subverse;
                        message.SubmissionID = submission.ID;
                        message.Type = Domain.Models.MessageType.SubmissionMention;
                        message.CreationDate = Repository.CurrentDate;

                        using (var repo = new Repository())
                        {
                            var response = await repo.SendMessage(message);

                            if (response.Success)
                            {
                                EventNotification.Instance.SendMentionNotice(recipient, submission.UserName, Domain.Models.ContentType.Submission, submission.ID, submission.Content);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static async Task SendCommentReplyNotification(Data.Models.Submission submission, Data.Models.Comment comment)
        {
            try
            {
                using (var _db = new voatEntities())
                {
                    Random _rnd = new Random();

                    if (comment.ParentID != null && comment.Content != null)
                    {
                        // find the parent comment and its author
                        var parentComment = _db.Comments.Find(comment.ParentID);
                        if (parentComment != null)
                        {
                            // check if recipient exists
                            if (UserHelper.UserExists(parentComment.UserName))
                            {
                                // do not send notification if author is the same as comment author
                                if (parentComment.UserName != comment.UserName)
                                {
                                    // send the message
                                    //BlockedUser Implementation - Comment Reply
                                    if (!MesssagingUtility.IsSenderBlocked(comment.UserName, parentComment.UserName))
                                    {
                                        //var submission = DataCache.Submission.Retrieve(comment.SubmissionID);
                                        //var q = new QuerySubmission(comment.SubmissionID.Value);
                                        //var submission = await q.ExecuteAsync().ConfigureAwait(false);

                                        if (submission != null)
                                        {
                                            //var subverse = DataCache.Subverse.Retrieve(submission.Subverse);

                                            var message = new Domain.Models.Message();

                                            message.IsAnonymized = submission.IsAnonymized;
                                            message.Recipient = parentComment.UserName;
                                            message.RecipientType = Domain.Models.IdentityType.User;
                                            message.Sender = comment.UserName;
                                            message.SenderType = Domain.Models.IdentityType.User;
                                            message.Subverse = submission.Subverse;
                                            message.SubmissionID = submission.ID;
                                            message.CommentID = comment.ID;
                                            message.Type = Domain.Models.MessageType.CommentReply;
                                            message.CreationDate = Repository.CurrentDate;

                                            using (var repo = new Repository())
                                            {
                                                var response = await repo.SendMessage(message);
                                                if (response.Success)
                                                {
                                                    EventNotification.Instance.SendMessageNotice(message.Recipient, message.Sender, Domain.Models.MessageTypeFlag.CommentReply, Domain.Models.ContentType.Comment, comment.ID);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task SendSubmissionReplyNotification(Data.Models.Submission submission, Data.Models.Comment comment)
        {
            try
            {
                // comment reply is sent to a root comment which has no parent id, trigger post reply notification
                //var submission = DataCache.Submission.Retrieve(comment.SubmissionID);
                //var q = new QuerySubmission(comment.SubmissionID.Value);
                //var submission = await q.ExecuteAsync().ConfigureAwait(false);
                
                if (submission != null)
                {
                    // check if recipient exists
                    if (UserHelper.UserExists(submission.UserName))
                    {
                        // do not send notification if author is the same as comment author
                        if (submission.UserName != comment.UserName)
                        {
                            //BlockedUser Implementation - Submission Reply
                            if (!MesssagingUtility.IsSenderBlocked(comment.UserName, submission.UserName))
                            {
                                var message = new Domain.Models.Message();

                                message.IsAnonymized = submission.IsAnonymized;
                                message.Recipient = submission.UserName;
                                message.RecipientType = Domain.Models.IdentityType.User;
                                message.Sender = comment.UserName;
                                message.SenderType = Domain.Models.IdentityType.User;
                                message.Subverse = submission.Subverse;
                                message.SubmissionID = submission.ID;
                                message.CommentID = comment.ID;
                                message.Type = Domain.Models.MessageType.SubmissionReply;
                                message.CreationDate = Repository.CurrentDate;

                                using (var repo = new Repository())
                                {
                                    var response = await repo.SendMessage(message);
                                    if (response.Success)
                                    {
                                        EventNotification.Instance.SendMessageNotice(message.Recipient, message.Sender, Domain.Models.MessageTypeFlag.CommentReply, Domain.Models.ContentType.Comment, comment.ID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static async Task SendCommentNotification(Data.Models.Submission submission, Data.Models.Comment comment)
        {
            if (comment.ParentID != null && comment.Content != null)
            {
                await SendCommentReplyNotification(submission, comment);
            }
            else
            {
                await SendSubmissionReplyNotification(submission, comment);
            }
        }
    }
}
