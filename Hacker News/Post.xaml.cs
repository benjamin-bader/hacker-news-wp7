﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace Hacker_News
{
    public class Comments
    {
        public string text { get; set; }
        public string title { get; set; }
        public string url { get; set; }
        public int id { get; set; }
        public int commentCount { get; set; }
        public int points { get; set; }
        public string postedAgo { get; set; }
        public string postedBy { get; set; }
        public List<Comment> comments { get; set; }
    }
    public class Comment
    {
        public string postedBy { get; set; }
        public string postedAgo { get; set; }
        public string comment { get; set; }
        public int id { get; set; }
        public int points { get; set; }
        public int parentId { get; set; }
        public int postId { get; set; }
        public List<Comment> children { get; set; }
    }

    public class FlatComments : INotifyPropertyChanged
    {
        private string textValue;
        private List<FlatComment> commentsValue = new List<FlatComment>();

        #region I don't really understand what's going on here ...
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion

        public string text
        {
            get { return this.textValue; }
            set { this.textValue = value; NotifyPropertyChanged("text"); }
        }

        public List<FlatComment> comments
        {
            get { return this.commentsValue; }
            set { this.commentsValue = value; NotifyPropertyChanged("comments"); }
        }
    }
    public class FlatComment
    {
        private int widthValue = 470;
        private int depthValue = 0;
        public string postedBy { get; set; }
        public string postedAgo { get; set; }
        public string comment { get; set; }
        public int id { get; set; }
        public int points { get; set; }
        public int parentId { get; set; }
        public int postId { get; set; }
        public int depth
        {
            get { return this.depthValue; }
            set
            {
                this.depthValue = value;
                this.widthValue = this.widthValue - value;
            }
        }
        public int width
        {
            get { return (this.widthValue); }
        }
    }

    public partial class Post : PhoneApplicationPage
    {
        public static int id;
        int depthIncrement = 20;
        public FlatComments flatComments = new FlatComments();

        private string stripHtml(string input)
        {
            Regex removeHtmlTags = new Regex("<(.|\n)*?>");
            return removeHtmlTags.Replace(input, " ").Trim();
        }

        public void setProgressBar(Boolean state)
        {
            progressBar.IsIndeterminate = state;
            progressBar.Visibility = (state == true) ? Visibility.Visible : Visibility.Collapsed;
        }

        FlatComment flattenComment(Comment input, int depth)
        {
            FlatComment output = new FlatComment();
            #region there HAS to be a better way to do this
            output.depth = depth;
            output.postedBy = input.postedBy;
            output.postedAgo = input.postedAgo;
            output.comment = stripHtml(input.comment);
            output.id = input.id;
            output.points = input.points;
            output.parentId = input.parentId;
            output.postId = input.postId;
            #endregion
            return output;
        }

        List<FlatComment> flattenComments(List<Comment> input, int currentDepth = 0)
        {
            List<FlatComment> output = new List<FlatComment>();
            Queue<Comment> queue = new Queue<Comment>(input);
            if (queue.Count == 0)
            {
                return output;
            }

            Comment car = queue.Dequeue();
            output.Add(flattenComment(car, currentDepth));
            if (car.children.Count > 0)
            {
                output.AddRange(flattenComments(car.children, currentDepth + depthIncrement));
            }

            List<Comment> cdr = new List<Comment>(queue);
            output.AddRange(flattenComments(cdr, currentDepth));

            return output;
        }

        private void HandleCommentResult(IAsyncResult result)
        {
            try
            {
                Common common = new Common();
                var binding = (result.AsyncState as AsyncState).binding as FlatComments;
                StreamReader txt = common.makeStreamReaderFromResult(result);

                Comments comments = common.deserializeStreamReader<Comments>(txt);
                this.Dispatcher.BeginInvoke(
                    () =>
                    {
                        textTextBlock.Text = stripHtml(comments.text);
                        binding.comments = flattenComments(comments.comments);
                        setProgressBar(false);
                    }
                );
            }
            catch (WebException e)
            {
                this.Dispatcher.BeginInvoke(
                    () =>
                    {
                        setProgressBar(false);
                    }
                );
            }
        }

        public void populateCommentsBinding(FlatComments binding, string Url)
        {
            AsyncState state = new AsyncState();
            HttpWebRequest request = HttpWebRequest.Create(Url) as HttpWebRequest;
            request.Accept = "application/json"; //atom+xml";
            state.request = request;
            state.binding = binding;
            request.BeginGetResponse(HandleCommentResult, state);
        }

        public Post()
        {
            InitializeComponent();
            setProgressBar(true);
            commentsList.DataContext = flatComments;
            string url = "http://api.ihackernews.com/post/" + id.ToString();
            populateCommentsBinding(flatComments, url);
        }
    }
}