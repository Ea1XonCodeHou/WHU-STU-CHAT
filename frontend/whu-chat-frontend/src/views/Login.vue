<template>
  <div class="login-container">
    <div class="background-shapes">
      <div class="shape shape-1"></div>
      <div class="shape shape-2"></div>
      <div class="shape shape-3"></div>
      <div class="shape shape-4"></div>
    </div>
    <div class="login-content">
      <div class="login-left">
        <div class="brand-container">
          <h1 class="brand-name">WHU-Chat</h1>
          <p class="brand-tagline">武汉大学学生互助平台</p>
        </div>
        <div class="value-props">
          <h2 class="value-title">校园互助交流一站式解决方案</h2>
          <div class="value-features">
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">forum</span></div>
              <div class="feature-text">实时聊天交流</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">groups</span></div>
              <div class="feature-text">学习互助小组</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">event_note</span></div>
              <div class="feature-text">校园活动分享</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">school</span></div>
              <div class="feature-text">学术资源共享</div>
            </div>
          </div>
        </div>
        <div class="brand-quote">
          <p>"连接校园，共创未来"</p>
        </div>
      </div>
      <div class="login-right">
        <div class="login-form-container">
          <div class="form-header">
            <h2 class="form-title">欢迎回来</h2>
            <p class="form-subtitle">登录账户开始你的校园社交</p>
          </div>
          <form class="login-form" @submit.prevent="handleLogin">
            <div class="form-group" :class="{ 'focused': activeField === 'username', 'filled': username }">
              <label for="username">用户名</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">person</span></span>
                <input 
                  type="text" 
                  id="username"
                  v-model="username"
                  @focus="setActiveField('username')"
                  @blur="setActiveField(null)"
                  autocomplete="username"
                >
              </div>
            </div>
            <div class="form-group" :class="{ 'focused': activeField === 'password', 'filled': password }">
              <label for="password">密码</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">lock</span></span>
                <input 
                  :type="showPassword ? 'text' : 'password'" 
                  id="password"
                  v-model="password"
                  @focus="setActiveField('password')"
                  @blur="setActiveField(null)"
                  autocomplete="current-password"
                >
                <button 
                  type="button" 
                  class="toggle-password"
                  @click="togglePasswordVisibility"
                  aria-label="Toggle password visibility"
                >
                  <span class="material-icon">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
            </div>
            
            <div v-if="errorMessage" class="error-message">
              <span class="material-icon">error_outline</span>
              <span>{{ errorMessage }}</span>
            </div>
            
            <div class="form-options">
              <label class="remember-me">
                <div class="custom-checkbox">
                  <input type="checkbox" v-model="rememberMe" id="remember">
                  <span class="checkmark"></span>
                </div>
                <span class="checkbox-label">记住我</span>
              </label>
              <a href="#" class="forgot-password">忘记密码?</a>
            </div>
            <button type="submit" class="login-button" :disabled="isLoading">
              <span v-if="isLoading" class="loader"></span>
              <span v-else>登录</span>
            </button>
            <div class="login-divider">
              <span>或</span>
            </div>
            <div class="alt-login-options">
              <button type="button" class="alt-login-btn sso-login">
                <span class="material-icon">school</span>
                <span>校园统一认证登录</span>
              </button>
            </div>
          </form>
          <div class="login-footer">
            <p>还没有账户? <router-link to="/register" class="register-link">注册账号</router-link></p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'LoginView',
  data() {
    return {
      username: '',
      password: '',
      rememberMe: false,
      showPassword: false,
      isLoading: false,
      activeField: null,
      errorMessage: ''
    }
  },
  mounted() {
    document.title = "WHU-Chat - 武汉大学学生互助平台 - 登录";
    this.addGlassMorphismEffect();
    window.addEventListener('mousemove', this.handleMouseMove);
    
    // 如果有记住的用户名，自动填充
    const rememberedUser = localStorage.getItem('rememberedUser');
    if (rememberedUser) {
      this.username = rememberedUser;
      this.rememberMe = true;
    }
  },
  beforeUnmount() {
    window.removeEventListener('mousemove', this.handleMouseMove);
  },
  methods: {
    handleLogin() {
      // 表单验证
      if (!this.username || !this.password) {
        this.errorMessage = '请输入用户名和密码';
        return;
      }
      
      this.isLoading = true;
      this.errorMessage = '';
      
      // 使用axios发送登录请求
      axios.post('/api/user/login', {
        username: this.username,
        password: this.password
      })
      .then(response => {
        this.isLoading = false;
        const data = response.data;
        
        if (data.code === 200) {
          // 调试日志
          console.log('登录成功，用户数据：', data.data);
          
          // 登录成功，存储token和用户信息
          localStorage.setItem('token', data.data.token);
          localStorage.setItem('userId', data.data.id);
          localStorage.setItem('userUsername', data.data.username);
          localStorage.setItem('userPhone', data.data.phone);
          localStorage.setItem('userEmail', data.data.email);
          localStorage.setItem('userAvatar', data.data.avatar);
          localStorage.setItem('userSelfDescription', data.data.self_description);
          
          // 将token添加到axios全局默认请求头中
          axios.defaults.headers.common['Authorization'] = `Bearer ${data.data.token}`;
          
          // 如果选择了记住我，可以额外存储一些信息
          if (this.rememberMe) {
            localStorage.setItem('rememberedUser', this.username);
          } else {
            localStorage.removeItem('rememberedUser');
          }
          
          // 跳转到首页
          this.$router.push('/home');
        } else {
          // 登录失败，显示错误信息
          this.errorMessage = data.msg || '登录失败，请检查用户名和密码';
        }
      })
      .catch(error => {
        this.isLoading = false;
        console.error('登录请求出错:', error);
        this.errorMessage = '网络错误，请稍后再试';
      });
    },
    togglePasswordVisibility() {
      this.showPassword = !this.showPassword;
    },
    setActiveField(field) {
      this.activeField = field;
    },
    addGlassMorphismEffect() {
      // 为背景形状添加随机位置
      const shapes = document.querySelectorAll('.shape');
      shapes.forEach(shape => {
        const randomDelay = Math.random() * 2;
        shape.style.animationDelay = `${randomDelay}s`;
      });
    },
    handleMouseMove(event) {
      // 视差效果
      const shapes = document.querySelectorAll('.shape');
      const x = event.clientX / window.innerWidth;
      const y = event.clientY / window.innerHeight;
      
      shapes.forEach((shape, index) => {
        const factor = (index + 1) * 15;
        const moveX = (x - 0.5) * factor;
        const moveY = (y - 0.5) * factor;
        shape.style.transform = `translate(${moveX}px, ${moveY}px)`;
      });
    }
  }
}
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700&display=swap');
@import url('https://fonts.googleapis.com/icon?family=Material+Icons');

/* 全局样式重置 */
* {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
}

.material-icon {
  font-family: 'Material Icons';
  font-weight: normal;
  font-style: normal;
  font-size: 24px;
  line-height: 1;
  letter-spacing: normal;
  text-transform: none;
  display: inline-block;
  white-space: nowrap;
  word-wrap: normal;
  direction: ltr;
  -webkit-font-feature-settings: 'liga';
  -webkit-font-smoothing: antialiased;
}

.login-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: linear-gradient(135deg, #8e44ad 0%, #3498db 100%);
  position: relative;
  overflow: hidden;
}

.background-shapes {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  overflow: hidden;
  z-index: 0;
}

.shape {
  position: absolute;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 50%;
  transition: transform 0.4s ease-out;
}

.shape-1 {
  width: 300px;
  height: 300px;
  top: -150px;
  left: -150px;
  background: linear-gradient(135deg, rgba(142, 68, 173, 0.2), rgba(41, 128, 185, 0.2));
  animation: floatAnimation 12s ease-in-out infinite alternate;
}

.shape-2 {
  width: 200px;
  height: 200px;
  bottom: -100px;
  right: -100px;
  background: linear-gradient(135deg, rgba(41, 128, 185, 0.2), rgba(142, 68, 173, 0.2));
  animation: floatAnimation 10s ease-in-out infinite alternate-reverse;
}

.shape-3 {
  width: 150px;
  height: 150px;
  bottom: 50%;
  left: 10%;
  background: rgba(255, 255, 255, 0.1);
  animation: floatAnimation 14s ease-in-out infinite alternate;
}

.shape-4 {
  width: 100px;
  height: 100px;
  top: 30%;
  right: 10%;
  background: rgba(255, 255, 255, 0.1);
  animation: floatAnimation 16s ease-in-out infinite alternate-reverse;
}

@keyframes floatAnimation {
  0% {
    transform: translate(0, 0) rotate(0deg);
  }
  100% {
    transform: translate(30px, 30px) rotate(10deg);
  }
}

.login-content {
  display: flex;
  max-width: 1200px;
  width: 100%;
  height: 700px;
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border-radius: 20px;
  overflow: hidden;
  box-shadow: 0 25px 50px rgba(0, 0, 0, 0.1);
}

.login-left {
  width: 50%;
  padding: 60px;
  background: linear-gradient(135deg, #9b59b6 0%, #3498db 100%);
  color: #fff;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  position: relative;
  overflow: hidden;
  clip-path: polygon(0 0, 100% 0, 93% 100%, 0% 100%);
  z-index: 1;
}

.brand-container {
  text-align: left;
  animation: fadeInDown 0.8s ease forwards;
}

.brand-name {
  font-size: 3rem;
  font-weight: 700;
  margin-bottom: 5px;
  color: #ffffff;
  text-shadow: 0 2px 10px rgba(0, 0, 0, 0.2);
}

.brand-tagline {
  font-size: 1.2rem;
  opacity: 0.9;
}

.value-props {
  margin-top: 30px;
  animation: fadeIn 0.8s ease forwards;
  animation-delay: 0.3s;
  opacity: 0;
}

.value-title {
  font-size: 1.8rem;
  font-weight: 600;
  margin-bottom: 30px;
  line-height: 1.3;
}

.value-features {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 25px;
}

.feature {
  display: flex;
  align-items: center;
  gap: 15px;
}

.feature-icon {
  width: 42px;
  height: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(255, 255, 255, 0.2);
  border-radius: 12px;
}

.feature-icon .material-icon {
  font-size: 22px;
  color: #ffffff;
}

.feature-text {
  font-size: 1.1rem;
  font-weight: 500;
}

.brand-quote {
  margin-top: 40px;
  font-style: italic;
  font-size: 1.3rem;
  opacity: 0.8;
  text-align: center;
  animation: fadeIn 0.8s ease forwards;
  animation-delay: 0.6s;
  opacity: 0;
}

.login-right {
  width: 50%;
  padding: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  background: rgba(255, 255, 255, 0.95);
}

.login-form-container {
  width: 100%;
  max-width: 380px;
  animation: fadeInRight 0.8s ease forwards;
}

.form-header {
  margin-bottom: 35px;
  text-align: center;
}

.form-title {
  font-size: 2rem;
  font-weight: 600;
  color: #333;
  margin-bottom: 10px;
}

.form-subtitle {
  color: #666;
  font-size: 1.1rem;
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  position: relative;
  margin-bottom: 5px;
}

.form-group label {
  position: absolute;
  left: 55px;
  top: 18px;
  color: #999;
  font-size: 16px;
  transition: all 0.3s ease;
  pointer-events: none;
}

.form-group.focused label, .form-group.filled label {
  top: -10px;
  left: 15px;
  font-size: 12px;
  color: #8e44ad;
  background: #fff;
  padding: 0 5px;
}

.input-container {
  display: flex;
  align-items: center;
  position: relative;
}

.input-icon {
  position: absolute;
  left: 15px;
  top: 50%;
  transform: translateY(-50%);
  color: #aaa;
  display: flex;
  align-items: center;
}

.input-icon .material-icon {
  font-size: 22px;
}

.form-group input {
  width: 100%;
  padding: 18px 20px 18px 50px;
  border: 1px solid #ddd;
  border-radius: 12px;
  font-size: 16px;
  background: #f8f9fa;
  color: #333;
  transition: all 0.3s ease;
}

.form-group input:focus {
  border-color: #8e44ad;
  background: #fff;
  box-shadow: 0 0 0 3px rgba(142, 68, 173, 0.1);
  outline: none;
}

.form-group.focused .input-icon, .form-group.filled .input-icon {
  color: #8e44ad;
}

.toggle-password {
  position: absolute;
  right: 15px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  color: #aaa;
  cursor: pointer;
  display: flex;
  align-items: center;
  padding: 0;
}

.toggle-password:hover {
  color: #8e44ad;
}

.toggle-password:focus {
  outline: none;
}

.form-options {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin: 5px 0;
}

.remember-me {
  display: flex;
  align-items: center;
  cursor: pointer;
  user-select: none;
}

.custom-checkbox {
  position: relative;
  width: 18px;
  height: 18px;
  margin-right: 8px;
}

.custom-checkbox input {
  position: absolute;
  opacity: 0;
  width: 0;
  height: 0;
}

.checkmark {
  position: absolute;
  top: 0;
  left: 0;
  width: 18px;
  height: 18px;
  background-color: #f1f1f1;
  border: 1px solid #ddd;
  border-radius: 4px;
  transition: all 0.2s ease;
}

.custom-checkbox input:checked ~ .checkmark {
  background-color: #8e44ad;
  border-color: #8e44ad;
}

.checkmark:after {
  content: "";
  position: absolute;
  display: none;
  left: 6px;
  top: 2px;
  width: 4px;
  height: 9px;
  border: solid white;
  border-width: 0 2px 2px 0;
  transform: rotate(45deg);
}

.custom-checkbox input:checked ~ .checkmark:after {
  display: block;
}

.checkbox-label {
  font-size: 14px;
  color: #666;
}

.forgot-password {
  font-size: 14px;
  color: #8e44ad;
  text-decoration: none;
  transition: all 0.2s ease;
}

.forgot-password:hover {
  color: #9b59b6;
  text-decoration: underline;
}

.login-button {
  width: 100%;
  padding: 16px;
  border: none;
  border-radius: 12px;
  background: linear-gradient(135deg, #9b59b6 0%, #3498db 100%);
  color: white;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
  margin-top: 10px;
  display: flex;
  justify-content: center;
  align-items: center;
}

.login-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 7px 14px rgba(0, 0, 0, 0.1);
}

.login-button:disabled {
  background: #ccc;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.error-message {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  background: #ffebee;
  color: #d32f2f;
  border-radius: 8px;
  margin-top: 5px;
  font-size: 14px;
  border-left: 3px solid #d32f2f;
}

.error-message .material-icon {
  margin-right: 10px;
  font-size: 20px;
}

.loader {
  display: inline-block;
  width: 24px;
  height: 24px;
  border: 3px solid rgba(0, 0, 0, 0.2);
  border-radius: 50%;
  border-top-color: #000000;
  animation: spin 1s ease-in-out infinite;
}

.login-divider {
  display: flex;
  align-items: center;
  color: #757575;
  font-size: 0.9rem;
  margin: 6px 0;
}

.login-divider::before,
.login-divider::after {
  content: "";
  flex: 1;
  border-bottom: 1px solid #ddd;
}

.login-divider::before {
  margin-right: 15px;
}

.login-divider::after {
  margin-left: 15px;
}

.alt-login-options {
  display: flex;
  justify-content: center;
  gap: 15px;
}

.alt-login-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 14px 20px;
  border-radius: 12px;
  background: #f8f9fa;
  border: 1px solid #ddd;
  color: #333;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  width: 100%;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
}

.alt-login-btn:hover {
  border-color: #8e44ad;
  background: #f1f1f1;
  transform: translateY(-2px);
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.alt-login-btn .material-icon {
  font-size: 20px;
  color: #8e44ad;
}

.login-footer {
  margin-top: 30px;
  text-align: center;
  color: #666;
  font-size: 0.95rem;
}

.login-footer a {
  color: #8e44ad;
  text-decoration: none;
  font-weight: 500;
}

.login-footer a:hover {
  text-decoration: underline;
  color: #9b59b6;
}

.register-link {
  color: #8e44ad;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
}

.register-link:hover {
  text-decoration: underline;
  color: #9b59b6;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

@keyframes fadeInDown {
  from {
    opacity: 0;
    transform: translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes fadeInRight {
  from {
    opacity: 0;
    transform: translateX(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

/* 响应式设计 */
@media (max-width: 1100px) {
  .login-content {
    max-width: 1000px;
  }
  
  .login-left {
    padding: 40px;
  }
  
  .feature-text {
    font-size: 1rem;
  }
}

@media (max-width: 992px) {
  .login-content {
    flex-direction: column;
    height: auto;
    min-height: auto;
    max-width: 600px;
  }
  
  .login-left {
    width: 100%;
    padding: 40px;
    clip-path: none;
  }
  
  .login-right {
    width: 100%;
    padding: 40px;
  }
  
  .login-form-container {
    width: 100%;
    padding: 0;
  }
  
  .value-features {
    grid-template-columns: 1fr 1fr;
  }
  
  .brand-quote {
    margin-top: 30px;
  }
}

@media (max-width: 576px) {
  .login-container {
    padding: 15px;
  }
  
  .login-left, .login-right {
    padding: 30px 20px;
  }
  
  .brand-name {
    font-size: 2.2rem;
  }
  
  .value-title {
    font-size: 1.6rem;
    margin-bottom: 25px;
  }
  
  .feature {
    gap: 10px;
  }
  
  .feature-icon {
    width: 36px;
    height: 36px;
  }
  
  .feature-icon .material-icon {
    font-size: 18px;
  }
  
  .feature-text {
    font-size: 0.9rem;
  }
  
  .form-title {
    font-size: 1.8rem;
  }
}
</style>
