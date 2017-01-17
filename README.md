# Pokemon_Super_Mystery_Dungeon
Pokemon Super Mystery Dungeon Tools<br>
《宝可梦超不可思议的迷宫》文本导出导入工具

## Describe & Usage
- msgtool
Message export/import console tool. 
Usage:
```
  -x, --extract        Extract binary talk messages.
  -c, --create         Create binary talk messages.
  -s, --script         Specific lua script file.
  -b, --binary-file    Specific binary file.
  -t, --text-file      Specific text file.
  -o, --output         Specific output file.
  --ctrl               Specific external control symbols file.
  -h, --help           Display this help screen.
```

- BPXJ Text Export
Message export GUI tool. 

- BPXJ Text Import
Message import GUI tool. 

- jukebox
Stand-along console tool for `jukebox.bin` message import/export. 

- farc.py
FARC archive unpacker. 
Usage:
```
farc.py [FILE INPUT]
```
For example, input `farc.py message.bin` in the console, `message.lst` should be place in the same directory as `message.bin`. 

## 简介和使用方法
- msgtool
命令行模式的文本导出/导入工具
用法:
```
  -x, --extract          从二进制文件导出文本
  -c, --create           创建二进制文本文件
  -s, --script            指定lua脚本文件路径
  -b, --binary-file    指定二进制文件路径
  -t, --text-file         指定文本文件路径
  -o, --output          指定输出文件路径
  --ctrl                     指定外部控制符文件
  -h, --help              显示帮助
```

- BPXJ Text Export
图形化的文本导出工具

- BPXJ Text Import
图形化的文本导入工具

- jukebox
专门用来导出/导入 `jukebox.bin` 中的文本

- farc.py
FARC 解包工具。
用法:
```
farc.py [FILE INPUT]
```
需要在输入文件同一目录下放置同名的“.lst”文件。例如当输入 `farc.py message.bin` 时，在`message.bin`文件相同的目录下应该存在一个`message.lst`文件，工具才能正确解包。这个“.lst”文件储存了FARC文件包中文件的文件名，不可缺少。


