# MSLX 插件项目

本项目基于 MSLX 官方插件模板生成。

- **后端**：基于 ASP.NET Core 与 [MSLX.SDK](https://github.com/MSLTeam/MSLX)。
- **前端**：基于 Vue 3 + TDesign + UnoCSS。
- **发布打包**：已配置 ILRepack，在 Release 模式下自动合并依赖 DLL。

## 开发指引

### 1. 前端开发
进入 `Frontend` 目录：
```bash
cd Frontend
pnpm install
pnpm dev:plugin
```

### 2. 后端开发与调试
```bash
dotnet build
```

在 Release 模式下构建将自动执行 ILRepack 合并：
```bash
dotnet build -c Release
```