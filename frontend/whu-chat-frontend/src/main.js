import axios from 'axios';
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import store from "./store"; // 导入Vuex store
import ElementPlus from 'element-plus';
import 'element-plus/dist/index.css';
import './style.css';
import './assets/css/dark-theme.css'; // 导入深色主题样式
import { initNotifications, playMessageSound, sendNotification } from './utils/notification'; // 导入通知工具

// 配置axios默认设置
axios.defaults.baseURL = 'http://localhost:5067'; // 使用原始的后端端口5067
axios.defaults.timeout = 1000000; // 请求超时时间
axios.defaults.headers.post['Content-Type'] = 'application/json;charset=UTF-8';

// 如果localStorage中有token，则在请求头中添加token
const token = localStorage.getItem('token');
if (token) {
  axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
}

// 请求拦截器
axios.interceptors.request.use(
  config => {
    // 添加请求日志
    console.log('发送请求:', {
      url: config.url,
      method: config.method,
      data: config.data,
      baseURL: config.baseURL,
      headers: config.headers
    });
    return config;
  },
  error => {
    // 处理请求错误
    console.error('请求配置错误:', error);
    return Promise.reject(error);
  }
);

// 响应拦截器
axios.interceptors.response.use(
  response => {
    // 添加响应日志
    console.log('收到响应:', {
      status: response.status,
      data: response.data,
      headers: response.headers
    });
    
    // 处理头像URL，确保完整路径
    if (response.data && response.data.avatar) {
      response.data.avatar = getFullAvatarUrl(response.data.avatar);
    }
    
    return response;
  },
  error => {
    // 添加错误响应日志
    console.error('响应错误:', {
      message: error.message,
      response: error.response ? {
        status: error.response.status,
        data: error.response.data
      } : 'No response',
      config: error.config
    });
    
    // 处理响应错误
    if (error.response && error.response.status === 401) {
      // 如果响应状态码是401（未授权），可能是token过期，清除token并跳转到登录页
      localStorage.removeItem('token');
      router.push('/login');
    }
    return Promise.reject(error);
  }
);

// 获取完整的头像URL
const getFullAvatarUrl = (avatarPath) => {
  if (!avatarPath) return null;
  if (avatarPath.startsWith('http')) return avatarPath;
  
  // 添加时间戳防止缓存
  const timestamp = new Date().getTime();
  const origin = window.location.origin;
  return avatarPath.startsWith('/') 
    ? `${origin}${avatarPath}?t=${timestamp}` 
    : `${origin}/${avatarPath}?t=${timestamp}`;
};

// 全局挂载方法
window.getFullAvatarUrl = getFullAvatarUrl;

const app = createApp(App);
app.use(router);
app.use(store); // 注册Vuex store
app.use(ElementPlus); // 注册ElementPlus

// 初始化从localStorage加载用户信息到Vuex
store.dispatch('user/initializeStore');

// 将axios挂载到全局
app.config.globalProperties.$axios = axios;

// 定义全局变量
window.apiBaseUrl = axios.defaults.baseURL;
window.allowMessageSound = true; // 默认启用消息音效
window.allowNotifications = true; // 默认启用桌面通知

// 添加通知音效
const messageSound = new Audio('/notify.mp3');
window.playMessageSound = (type = 'private') => {
  // 检查全局设置
  if (!window.allowMessageSound) return;
  
  // 检查特定类型的免打扰设置
  if (type === 'private' && window.privateChatMute) return;
  if (type === 'group' && window.groupChatMute) return;
  if (type === 'system' && window.systemNotificationMute) return;
  
  if (messageSound) {
    messageSound.currentTime = 0;
    messageSound.play().catch(err => console.log('无法播放通知音效:', err));
  }
};

// 添加浏览器通知
window.showNotification = (title, body, type = 'private') => {
  // 检查全局设置
  if (!window.allowNotifications) return;
  
  // 检查特定类型的免打扰设置
  if (type === 'private' && window.privateChatMute) return;
  if (type === 'group' && window.groupChatMute) return;
  if (type === 'system' && window.systemNotificationMute) return;
  
  if (Notification && Notification.permission === 'granted') {
    new Notification(title, { body });
  } else if (Notification && Notification.permission !== 'denied') {
    Notification.requestPermission().then(permission => {
      if (permission === 'granted') {
        new Notification(title, { body });
      }
    });
  }
};

// 加载用户设置
const loadUserSettings = () => {
  // 在线状态设置
  window.showMyOnlineStatus = localStorage.getItem('setting_showMyOnlineStatus') !== 'false';
};

app.mount("#app");

// 初始化设置
loadUserSettings();
// 初始化通知系统
initNotifications();