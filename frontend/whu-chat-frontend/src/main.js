import axios from 'axios';
import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";

// 配置axios默认设置
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
    // 在请求发送前可以做一些处理，如显示加载动画等
    return config;
  },
  error => {
    // 处理请求错误
    return Promise.reject(error);
  }
);

// 响应拦截器
axios.interceptors.response.use(
  response => {
    // 如果返回的状态码为200，说明请求成功，可以正常拿到数据
    return response;
  },
  error => {
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