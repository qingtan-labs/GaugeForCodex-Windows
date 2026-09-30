namespace GaugeForCodex;

public static class Text
{
    public static string Language { get; set; } = "zh";
    // Every key has English, Simplified Chinese, Japanese and Spanish counterparts.
    internal static readonly Dictionary<string, string[]> Strings = new()
    {
        ["widget"] = ["Show desktop widget", "显示桌面组件", "デスクトップに表示", "Mostrar componente de escritorio"],
        ["firstRun"] = ["Click this tray icon to see quota. If hidden, drag it out of the Windows tray overflow.", "点击右下角图标查看额度；若图标在小箭头内，可拖到托盘常显。", "トレイアイコンをクリックすると使用枠を表示します。隠れている場合はトレイからドラッグしてください。", "Pulse el icono de la bandeja. Si está oculto, arrástrelo desde el menú de iconos."],
        ["settingsFailed"] = ["Could not save startup settings. Keep the app in a writable folder.", "无法保存启动设置，请确保安装目录可写。", "起動設定を保存できません。書き込み可能なフォルダーを使用してください。", "No se guardó la configuración. Use una carpeta con permiso de escritura."],
        ["rolledBack"] = ["The update could not complete. The previous version has been restored.", "安装未完成，已恢复到之前的版本。", "更新できなかったため、以前のバージョンに戻しました。", "La actualización no se completó. Se restauró la versión anterior."],
        ["title"] = ["Codex quota", "Codex 额度", "Codex 使用枠", "Cuota de Codex"],
        ["remainingCaption"] = ["remaining", "剩余额度", "残り", "disponible"],
        ["widgetHint"] = ["Frosted glass · drag to move", "雾面玻璃 · 可拖动位置", "すりガラス・ドラッグで移動", "Cristal mate · arrastrar para mover"],
        ["detailsOpen"] = ["View details", "查看详情", "詳細を表示", "Ver detalles"],
        ["detailsClose"] = ["Less details", "收起详情", "詳細を閉じる", "Ocultar detalles"],
        ["syncShort"] = ["Refresh", "刷新额度", "更新", "Actualizar"],
        ["updatesShort"] = ["Updates", "检查更新", "アップデート", "Actualizaciones"],
        ["updateAvailable"] = ["Update available", "发现新版本", "新バージョン", "Nueva versión"],
        ["settingsShort"] = ["Settings", "设置", "設定", "Preferencias"],
        ["show"] = ["Show quota card", "显示额度卡片", "カードを表示", "Mostrar tarjeta"],
        ["hide"] = ["Hide to tray", "隐藏到托盘", "トレイに隠す", "Ocultar en bandeja"],
        ["sync"] = ["Refresh quota now", "立即刷新额度", "使用枠を更新", "Actualizar cuota"],
        ["manual"] = ["Enter quota manually…", "手动填写额度…", "手動で入力…", "Introducir cuota…"],
        ["reset"] = ["Reset card position", "重置卡片位置", "位置をリセット", "Restablecer posición"],
        ["pin"] = ["Always on top", "置于顶层", "最前面に表示", "Siempre encima"],
        ["settings"] = ["Settings…", "设置…", "設定…", "Preferencias…"],
        ["updates"] = ["Check for updates…", "检查更新…", "更新を確認…", "Buscar actualizaciones…"],
        ["about"] = ["About Gauge for Codex…", "关于 Gauge for Codex…", "Gauge for Codex について…", "Acerca de Gauge for Codex…"],
        ["exit"] = ["Exit application", "退出应用", "アプリを終了", "Salir de la aplicación"],
        ["remaining"] = ["{0}% remaining", "剩余 {0}%", "残り {0}%", "{0}% restante"],
        ["used"] = ["{0}% used", "已用 {0}%", "使用済み {0}%", "{0}% utilizado"],
        ["unknown"] = ["Quota unavailable", "额度暂不可用", "使用枠を取得できません", "Cuota no disponible"],
        ["manualHint"] = ["Right-click to enter quota", "右键可手动填写", "右クリックで手動入力", "Clic derecho para introducir cuota"],
        ["noData"] = ["No available quota data", "当前没有可读取的额度数据", "使用枠データがありません", "No hay datos de cuota"],
        ["resetUnknown"] = ["Reset time unknown", "重置时间未知", "リセット日時は不明", "Reinicio desconocido"],
        ["expired"] = ["Reset passed · refresh required", "重置时刻已过 · 待刷新", "リセット済み・更新が必要", "Reinicio pasado · actualice"],
        ["today"] = ["Resets today · {0}", "今天重置 · {0}", "今日リセット · {0}", "Reinicio hoy · {0}"],
        ["tomorrow"] = ["Resets tomorrow · {0}", "明天重置 · {0}", "明日リセット · {0}", "Reinicio mañana · {0}"],
        ["days"] = ["Resets in {0} days · {1}", "{0} 天后重置 · {1}", "{0}日後リセット · {1}", "Reinicio en {0} días · {1}"],
        ["dayPeriod"] = ["{0}-day quota", "{0} 天额度", "{0}日使用枠", "Cuota de {0} días"],
        ["hourPeriod"] = ["{0}-hour quota", "{0} 小时额度", "{0}時間使用枠", "Cuota de {0} horas"],
        ["minutePeriod"] = ["{0}-minute quota", "{0} 分钟额度", "{0}分使用枠", "Cuota de {0} minutos"],
        ["period"] = ["Quota window", "额度周期", "使用枠の期間", "Periodo de cuota"],
        ["updated"] = ["Updated {0}", "更新于 {0}", "更新 {0}", "Actualizado {0}"],
        ["cached"] = ["Cached · {0}", "缓存数据 · {0}", "キャッシュ · {0}", "En caché · {0}"],
        ["manualValue"] = ["Manual values", "手动填写", "手動入力", "Valores manuales"],
        ["tooltip"] = ["Drag to move · click for details · right-click for menu", "拖动调整位置 · 单击查看详情 · 右键打开菜单", "ドラッグで移動・クリックで詳細・右クリックでメニュー", "Arrastrar para mover · clic para detalles · clic derecho para menú"],
        ["tooltipSingle"] = ["Drag to move · right-click for menu", "拖动调整位置 · 右键打开菜单", "ドラッグで移動・右クリックでメニュー", "Arrastrar para mover · clic derecho para menú"],
        ["otherQuotaWarning"] = ["{0} · only {1}% remaining", "{0} · 仅剩 {1}%", "{0} · 残り {1}%", "{0} · solo queda {1}%"],
        ["otherExhausted"] = ["{0} exhausted · wait for reset", "{0}已耗尽 · 请等待重置", "{0}を使い切りました · リセットを待機", "{0} agotada · espere al reinicio"],
        ["follow"] = ["Start and stop with Codex", "跟随 Codex 启动和退出", "Codex と同時に起動・終了", "Iniciar y salir con Codex"],
        ["login"] = ["Start independently at Windows sign-in", "登录 Windows 时独立启动", "Windows ログイン時に独立起動", "Iniciar al acceder a Windows"],
        ["weekly"] = ["Check weekly (notify only)", "每周检查更新（仅提醒）", "週1回更新を確認（通知のみ）", "Buscar cada semana (solo avisar)"],
        ["tint"] = ["Desktop glass tint · readable range", "桌面玻璃底色浓度 · 保持文字可读", "デスクトップガラス濃度・読みやすい範囲", "Tinte del cristal de escritorio · rango legible"],
        ["save"] = ["Save", "保存", "保存", "Guardar"],
        ["cancel"] = ["Cancel", "取消", "キャンセル", "Cancelar"],
        ["resetDate"] = ["Reset date and time (local)", "重置日期和时间（本地时区）", "リセット日時（現地時間）", "Fecha y hora de reinicio (local)"],
        ["invalid"] = ["Enter 0–100 and a valid future date/time.", "请输入 0–100 的百分比及有效的未来日期时间。", "0〜100 と有効な未来の日時を入力してください。", "Introduzca 0–100 y una fecha futura válida."],
        ["checking"] = ["Checking for updates…", "正在检查更新…", "更新を確認中…", "Buscando actualizaciones…"],
        ["latest"] = ["You are up to date ({0}).", "已经是最新版本（{0}）。", "最新版です（{0}）。", "Ya tiene la versión más reciente ({0})."],
        ["checkFailed"] = ["Could not check for updates. Try again from the tray menu.", "暂时无法检查更新，请稍后从托盘菜单重试。", "更新を確認できません。トレイから再試行してください。", "No se pudieron buscar actualizaciones. Reintente desde la bandeja."],
        ["newVersion"] = ["Version {0} is available", "发现新版本 {0}", "新バージョン {0}", "Versión {0} disponible"],
        ["install"] = ["Download and install", "下载并安装", "ダウンロードしてインストール", "Descargar e instalar"],
        ["installing"] = ["Downloading and verifying…", "正在下载并校验…", "ダウンロード・検証中…", "Descargando y verificando…"],
        ["installFailed"] = ["Update failed. Your current installation is unchanged.", "更新未完成，当前安装保持不变。", "更新できませんでした。現在のインストールは変更されていません。", "La actualización falló. La instalación actual no cambió."],
        ["installWarning"] = ["Unsigned community build. Installs only after SHA-256 verification; retains settings and a rollback copy.", "社区构建尚未进行代码签名；通过 SHA-256 校验后安装，保留设置及回退副本。", "未署名のコミュニティビルド。SHA-256 検証後にインストールし、設定とバックアップを保持します。", "Compilación comunitaria sin firma. Verifica SHA-256 y conserva preferencias y copia de recuperación."],
        ["aboutText"] = ["Gauge for Codex · Windows {0}\nIndependent community tool. Not affiliated with or endorsed by OpenAI.\nQuota refresh does not invoke a model. Updates contact GitHub. No analytics or telemetry.", "Gauge for Codex · Windows {0}\n独立社区工具，与 OpenAI 没有隶属或背书关系。\n额度刷新不调用模型；检查更新访问 GitHub；无分析统计或遥测。", "Gauge for Codex · Windows {0}\nOpenAI と無関係の非公式コミュニティツール。\n使用枠の更新はモデルを呼び出しません。更新確認は GitHub に接続します。解析やテレメトリなし。", "Gauge for Codex · Windows {0}\nHerramienta comunitaria independiente, no afiliada ni respaldada por OpenAI.\nConsultar cuota no invoca modelos. Las actualizaciones acceden a GitHub. Sin analítica ni telemetría."]
    };
    public static string Get(string key, params object[] values)
    {
        var index = Language switch { "zh" => 1, "ja" => 2, "es" => 3, _ => 0 };
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, Strings[key][index], values);
    }
}
