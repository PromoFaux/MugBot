# MugBot
This is a single-purpose bot used to notify a channel on Mattermost that a user has had their pull request merged for the first time.

[![Codacy Badge](https://api.codacy.com/project/badge/Grade/a4cd88f8a39d4268bf9dd2db1c25f92f)](https://www.codacy.com/app/promofaux/MugBot?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=PromoFaux/MugBot&amp;utm_campaign=Badge_Grade)
[![Docker Build Status](https://img.shields.io/docker/build/promofaux/mugbot.svg)](https://hub.docker.com/r/promofaux/mugbot/builds/) [![Docker Stars](https://img.shields.io/docker/stars/promofaux/mugbot.svg)](https://hub.docker.com/r/promofaux/mugbot/) [![Docker Pulls](https://img.shields.io/docker/pulls/promofaux/mugbot.svg)](https://hub.docker.com/r/promofaux/mugbot/) 

![](https://i.imgur.com/AC4iYjv.gif)

## Deployment
Recommended - Use pre-built container:
- Create a directory to store the bot's config file, e.g `/opt/bot/mugbot` (`${YOUR_DIRECTORY}`)
- Create the config file in `${YOUR_DIRECTORY}`. Read below for details

```BASH
docker run -d --restart=always \
           -v ${YOUR_DIRECTORY}:/config/ \
           -p ${YOUR_PORT}:80 --name mugbot \
           -u `id -u ${USER}`:`id -g ${USER}`\
           promofaux/mugbot:latest
```
`${YOUR_PORT}` is the port your bot will be listening on

(There are probably better ways to do the user bit, but I'm a novice at Docker - any hints welcomed!)

On the Github side of things, add a webhook to your repository with a `Content Type` of `application/json`, then choose the `Let me select individual events` radio button, and make sure that only `Pull request` is ticked.


## Configuration

```JSON
{
  "MmConfig": {
    "WebhookUrl": "Your incoming webhook URL",
    "Channel": "town-square",
    "Username": "MugBot",
    "IconUrl": "Image URL for bot icon"
  },
  "Secret": "github Secret configured in webhook", 
  "CelebrationEmoji": "optional emjoji e.g :celebrate:",
  "CustomString": "@promofaux - Please send them a mug!",
  "IgnoredUsers": [ 
    "PromoFaux"  
  ]
}
```

`CelebrationEmoji` and `CustomString` can be null.

The `IgnoredUsers` array will be automatically populated as new users PRs are merged, so as not to send out duplicate messages.
