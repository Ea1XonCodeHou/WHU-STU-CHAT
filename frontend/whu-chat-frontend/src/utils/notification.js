/**
 * 通知工具类
 */

// 消息提示音
const messageSound = new Audio('/notify.mp3');

/**
 * 播放消息音效
 * @param {string} type 消息类型：private, group, system
 */
export function playMessageSound(type = 'private') {
  try {
    // 检查是否允许播放声音
    if (localStorage.getItem('setting_messageSound') !== 'true') {
      return;
    }
    
    // 检查特定类型的免打扰设置
    if (type === 'private' && localStorage.getItem('setting_privateChatMute') === 'true') {
      return;
    }
    
    if (type === 'group' && localStorage.getItem('setting_groupChatMute') === 'true') {
      return;
    }
    
    if (type === 'system' && localStorage.getItem('setting_systemNotificationMute') === 'true') {
      return;
    }
    
    // 重置音频进度并播放
    messageSound.currentTime = 0;
    messageSound.play().catch(err => {
      console.error('播放音效失败:', err);
    });
  } catch (err) {
    console.error('音效播放错误:', err);
  }
}

/**
 * 发送桌面通知
 * @param {string} title 通知标题
 * @param {string} body 通知内容
 * @param {string} icon 通知图标
 * @param {Function} onClick 点击通知的回调函数
 * @param {string} type 消息类型：private, group, system
 */
export function sendNotification(title, body, icon = null, onClick = null, type = 'private') {
  try {
    // 检查是否允许发送通知
    if (localStorage.getItem('setting_newMessageNotification') !== 'true') {
      return;
    }
    
    // 检查特定类型的免打扰设置
    if (type === 'private' && localStorage.getItem('setting_privateChatMute') === 'true') {
      return;
    }
    
    if (type === 'group' && localStorage.getItem('setting_groupChatMute') === 'true') {
      return;
    }
    
    if (type === 'system' && localStorage.getItem('setting_systemNotificationMute') === 'true') {
      return;
    }
    
    // 检查浏览器是否支持通知
    if (!('Notification' in window)) {
      console.warn('当前浏览器不支持桌面通知');
      return;
    }
    
    // 如果用户已经授权，直接发送通知
    if (Notification.permission === 'granted') {
      createNotification(title, body, icon, onClick);
    }
    // 如果用户未拒绝授权，请求授权
    else if (Notification.permission !== 'denied') {
      Notification.requestPermission().then(permission => {
        if (permission === 'granted') {
          createNotification(title, body, icon, onClick);
        }
      });
    }
  } catch (err) {
    console.error('发送通知错误:', err);
  }
}

/**
 * 创建通知实例
 */
function createNotification(title, body, icon, onClick) {
  const notification = new Notification(title, {
    body: body,
    icon: icon || '/favicon.ico',
    silent: true // 不使用系统声音，我们自己控制声音
  });
  
  if (onClick && typeof onClick === 'function') {
    notification.onclick = () => {
      window.focus();
      onClick();
      notification.close();
    };
  }
  
  // 自动关闭通知
  setTimeout(() => {
    notification.close();
  }, 5000);
}

/**
 * 请求浏览器通知权限
 */
export function requestNotificationPermission() {
  if (!('Notification' in window)) {
    console.warn('当前浏览器不支持桌面通知');
    return Promise.resolve(false);
  }
  
  return Notification.requestPermission()
    .then(permission => {
      return permission === 'granted';
    })
    .catch(error => {
      console.error('请求通知权限失败:', error);
      return false;
    });
}

/**
 * 初始化通知工具
 */
export function initNotifications() {
  // 将方法绑定到全局，方便在组件外部调用
  window.playMessageSound = (type) => playMessageSound(type);
  window.sendNotification = (title, body, icon, onClick, type) => 
    sendNotification(title, body, icon, onClick, type);
  
  // 设置默认状态
  window.showMyOnlineStatus = localStorage.getItem('setting_showMyOnlineStatus') !== 'false';
} 