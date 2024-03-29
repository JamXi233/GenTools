﻿using Newtonsoft.Json.Linq;
using System.Linq;
using System.IO;
using Windows.Storage;
using System;
using Newtonsoft.Json;
using Spectre.Console;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace GenTools.Depend
{
    public class GetNotify
    {
        public string post_id { get; set; }
        public string type { get; set; }
        public string tittle { get; set; }
        public string url { get; set; }
        public string show_time { get; set; }
        public string order { get; set; }
        public string title { get; set; }

        public async Task Get()
        {
            string apiAddress = "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18";

            using (HttpClient client = new HttpClient())
            {
                // 使用 HttpClient 发送请求并获取响应
                string jsonResponse = await client.GetStringAsync(apiAddress);

                // 将API响应转换为JSON对象并筛选特定类型的帖子
                var jsonObject = JObject.Parse(jsonResponse);
                var activityPosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ACTIVITY")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var announcePosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_ANNOUNCE")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();
                var infoPosts = jsonObject["data"]["post"]
                    .Where(p => (string)p["type"] == "POST_TYPE_INFO")
                    .OrderByDescending(p => (string)p["type"])
                    .ToList();

                // 将结果保存到文件中
                var folder = KnownFolders.DocumentsLibrary;
                var GenToolsFolder = await folder.CreateFolderAsync("JSG-LLC\\GenTools", CreationCollisionOption.OpenIfExists);
                var activityFile = await GenToolsFolder.CreateFileAsync("Posts\\activity.json", CreationCollisionOption.OpenIfExists);
                var announceFile = await GenToolsFolder.CreateFileAsync("Posts\\announce.json", CreationCollisionOption.OpenIfExists);
                var infoFile = await GenToolsFolder.CreateFileAsync("Posts\\info.json", CreationCollisionOption.OpenIfExists);
                await File.WriteAllTextAsync(activityFile.Path, JArray.FromObject(activityPosts).ToString());
                await File.WriteAllTextAsync(announceFile.Path, JArray.FromObject(announcePosts).ToString());
                await File.WriteAllTextAsync(infoFile.Path, JArray.FromObject(infoPosts).ToString());
            }
        }

        public List<GetNotify> GetData(string localData)
        {
            var records = JsonConvert.DeserializeObject<List<GetNotify>>(localData);
            return records;
        }
    }
}
