# CloudDrive

#### 介绍
基于官方公开API搭建的文件同步工具，无加速功能。

#### 软件架构说明
![输入图片说明](https://foruda.gitee.com/images/1683522780884123685/7c25d7ba_8170626.jpeg "00.jpg")

#### 使用说明
- 解压直接运行 CloudDriveUI.exe 文件
- 首次允许需要申请授权许可

#### 框架说明
- WPF .Net Core 7
- Nlog 5
- Prism 8
- MaterialDesignThemes 4

#### 功能说明
- 网盘文件管理功能：上传、下载、删除、重命名等
- 文件同步功能：文件同步、定时同步、同步文件过滤等
- 回收站功能：删除以及同步更新时的原始文件保存在 .backup 文件夹下，实现找回功能。后期更新UI界面。
- 软件只实现了百度网盘API，无加速功能；扩展其他网盘API可实现 ICloudDriveProvider 接口并注入到容器。

#### 免责声明
- 本软件为免费开源项目，仅供学习交流，无任何形式的盈利行为。
- 本软件服务于百度网盘，如有侵权，请与我联系，会及时处理。
- 本软件皆调用官方接口实现，无破坏官方接口行为。
- 本软件仅做数据转发，不拦截、存储、篡改任何用户数据。
- 严禁使用本软件进行盈利、损坏官方、散落任何违法信息等行为。
- 本软件不作任何稳定性的承诺，如因使用本软件导致的文件丢失、文件破坏等意外情况，均与本软件无关。

