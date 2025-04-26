import axios from 'axios';
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";

// 配置axios默认设置
axios.defaults.baseURL = 'http://localhost:5067'; // 更新为实际后端API地址
axios.defaults.timeout = 10000; // 请求超时时间
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

const app = createApp(App);
app.use(router);

// 将axios挂载到全局
app.config.globalProperties.$axios = axios;

app.mount("#app");