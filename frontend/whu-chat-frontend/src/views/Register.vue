<template>
  <div class="register-container">
    <div class="background-shapes">
      <div class="shape shape-1"></div>
      <div class="shape shape-2"></div>
      <div class="shape shape-3"></div>
      <div class="shape shape-4"></div>
    </div>
    <div class="register-content">
      <div class="register-left">
        <div class="brand-container">
          <h1 class="brand-name">WHU-Chat</h1>
          <p class="brand-tagline">武汉大学学生互助平台</p>
        </div>
        <div class="value-props">
          <h2 class="value-title">加入校园互助社区</h2>
          <div class="value-features">
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">chat</span></div>
              <div class="feature-text">即时交流讨论</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">library_books</span></div>
              <div class="feature-text">学习资料共享</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">diversity_3</span></div>
              <div class="feature-text">结交志同道合好友</div>
            </div>
            <div class="feature">
              <div class="feature-icon"><span class="material-icon">volunteer_activism</span></div>
              <div class="feature-text">校园互帮互助</div>
            </div>
          </div>
        </div>
        <div class="brand-quote">
          <p>"让校园生活更精彩，一起成长"</p>
        </div>
      </div>
      <div class="register-right">
        <div class="register-form-container">
          <div class="form-header">
            <h2 class="form-title">创建账户</h2>
            <p class="form-subtitle">注册加入武汉大学校园互助社区</p>
          </div>
          <form class="register-form" @submit.prevent="handleRegister">
            <!-- 用户名 -->
            <div class="form-group" :class="{ 'focused': activeField === 'username', 'filled': formData.username, 'error': errors.username }">
              <label for="username">用户名</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">person</span></span>
                <input 
                  type="text" 
                  id="username"
                  v-model="formData.username"
                  @focus="setActiveField('username')"
                  @blur="validateField('username')"
                  autocomplete="username"
                >
              </div>
              <div class="error-message" v-if="errors.username">{{ errors.username }}</div>
            </div>
            
            <!-- 邮箱 -->
            <div class="form-group" :class="{ 'focused': activeField === 'email', 'filled': formData.email, 'error': errors.email }">
              <label for="email">电子邮箱</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">email</span></span>
                <input 
                  type="email" 
                  id="email"
                  v-model="formData.email"
                  @focus="setActiveField('email')"
                  @blur="validateField('email')"
                  autocomplete="email"
                >
              </div>
              <div class="error-message" v-if="errors.email">{{ errors.email }}</div>
            </div>
            
            <!-- 手机号 -->
            <div class="form-group" :class="{ 'focused': activeField === 'phone', 'filled': formData.phone, 'error': errors.phone }">
              <label for="phone">手机号码</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">phone</span></span>
                <input 
                  type="tel" 
                  id="phone"
                  v-model="formData.phone"
                  @focus="setActiveField('phone')"
                  @blur="validateField('phone')"
                  autocomplete="tel"
                >
              </div>
              <div class="error-message" v-if="errors.phone">{{ errors.phone }}</div>
            </div>

            <!-- 密码 -->
            <div class="form-group" :class="{ 'focused': activeField === 'password', 'filled': formData.password, 'error': errors.password }">
              <label for="password">密码</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">lock</span></span>
                <input 
                  :type="showPassword ? 'text' : 'password'" 
                  id="password"
                  v-model="formData.password"
                  @focus="setActiveField('password')"
                  @blur="validateField('password')"
                  autocomplete="new-password"
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
              <div class="error-message" v-if="errors.password">{{ errors.password }}</div>
              <div class="password-strength" v-if="formData.password">
                <div class="strength-text">密码强度: <span :class="passwordStrengthClass">{{ passwordStrengthLabel }}</span></div>
                <div class="strength-bar">
                  <div class="strength-indicator" :style="{ width: passwordStrengthPercentage + '%', backgroundColor: passwordStrengthColor }"></div>
                </div>
              </div>
            </div>
            
            <!-- 确认密码 -->
            <div class="form-group" :class="{ 'focused': activeField === 'confirmPassword', 'filled': formData.confirmPassword, 'error': errors.confirmPassword }">
              <label for="confirmPassword">确认密码</label>
              <div class="input-container">
                <span class="input-icon"><span class="material-icon">lock_clock</span></span>
                <input 
                  :type="showConfirmPassword ? 'text' : 'password'" 
                  id="confirmPassword"
                  v-model="formData.confirmPassword"
                  @focus="setActiveField('confirmPassword')"
                  @blur="validateField('confirmPassword')"
                  autocomplete="new-password"
                >
                <button 
                  type="button" 
                  class="toggle-password"
                  @click="toggleConfirmPasswordVisibility"
                  aria-label="Toggle confirm password visibility"
                >
                  <span class="material-icon">{{ showConfirmPassword ? 'visibility_off' : 'visibility' }}</span>
                </button>
              </div>
              <div class="error-message" v-if="errors.confirmPassword">{{ errors.confirmPassword }}</div>
            </div>

            <div class="form-options">
              <label class="accept-terms">
                <div class="custom-checkbox">
                  <input type="checkbox" v-model="formData.agreeTerms" id="terms" @change="validateField('agreeTerms')">
                  <span class="checkmark"></span>
                </div>
                <span class="checkbox-label">我同意 <a href="#" class="terms-link">服务条款</a> 和 <a href="#" class="terms-link">隐私政策</a></span>
              </label>
            </div>
            <div class="error-message terms-error" v-if="errors.agreeTerms">{{ errors.agreeTerms }}</div>
            
            <button type="submit" class="register-button" :disabled="isLoading || !isValidForm">
              <span v-if="isLoading" class="loader"></span>
              <span v-else>注册账号</span>
            </button>

            <!-- 错误消息 -->
            <div v-if="errorMessage" class="error-message global-error">
              <span class="material-icon">error_outline</span>
              <span>{{ errorMessage }}</span>
            </div>
            
            <!-- 成功消息 -->
            <div v-if="successMessage" class="success-message">
              <span class="material-icon">check_circle_outline</span>
              <span>{{ successMessage }}</span>
            </div>
          </form>
          <div class="register-footer">
            <p>已有账户? <router-link to="/login" class="login-link">登录</router-link></p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from 'axios';

export default {
  name: 'RegisterView',
  data() {
    return {
      formData: {
        username: '',
        email: '',
        phone: '',
        password: '',
        confirmPassword: '',
        agreeTerms: false
      },
      errors: {
        username: '',
        email: '',
        phone: '',
        password: '',
        confirmPassword: '',
        agreeTerms: ''
      },
      showPassword: false,
      showConfirmPassword: false,
      isLoading: false,
      activeField: null,
      errorMessage: '',
      successMessage: ''
    }
  },
  computed: {
    passwordStrength() {
      const password = this.formData.password;
      
      if (!password) return 0;
      if (password.length < 6) return 1;
      
      let score = 0;
      
      // Length
      if (password.length >= 8) score += 1;
      if (password.length >= 10) score += 1;
      
      // Complexity
      if (/[A-Z]/.test(password)) score += 1;
      if (/[a-z]/.test(password)) score += 1;
      if (/[0-9]/.test(password)) score += 1;
      if (/[^A-Za-z0-9]/.test(password)) score += 1;
      
      return Math.min(5, score);
    },
    passwordStrengthLabel() {
      const strengthLabels = ['', '非常弱', '弱', '中等', '强', '非常强'];
      return strengthLabels[this.passwordStrength];
    },
    passwordStrengthClass() {
      const strengthClasses = ['', 'very-weak', 'weak', 'medium', 'strong', 'very-strong'];
      return strengthClasses[this.passwordStrength];
    },
    passwordStrengthPercentage() {
      return (this.passwordStrength / 5) * 100;
    },
    passwordStrengthColor() {
      const colors = [
        '',
        '#ff4d4d', // 非常弱 - 红色
        '#ff9933', // 弱 - 橙色
        '#ffcc00', // 中等 - 黄色
        '#66cc33', // 强 - 绿色
        '#33cc66'  // 非常强 - 深绿色
      ];
      return colors[this.passwordStrength];
    },
    isValidForm() {
      return (
        this.formData.username &&
        this.formData.email &&
        this.formData.phone &&
        this.formData.password &&
        this.formData.confirmPassword &&
        this.formData.agreeTerms &&
        !this.errors.username &&
        !this.errors.email &&
        !this.errors.phone &&
        !this.errors.password &&
        !this.errors.confirmPassword &&
        !this.errors.agreeTerms
      );
    }
  },
  mounted() {
    document.title = "WHU-Chat - 武汉大学学生互助平台 - 注册";
    this.addGlassMorphismEffect();
    window.addEventListener('mousemove', this.handleMouseMove);
  },
  beforeUnmount() {
    window.removeEventListener('mousemove', this.handleMouseMove);
  },
  methods: {
    setActiveField(field) {
      this.activeField = field;
    },
    togglePasswordVisibility() {
      this.showPassword = !this.showPassword;
    },
    toggleConfirmPasswordVisibility() {
      this.showConfirmPassword = !this.showConfirmPassword;
    },
    validateField(field) {
      if (field === 'username') this.validateUsername();
      else if (field === 'email') this.validateEmail();
      else if (field === 'phone') this.validatePhone();
      else if (field === 'password') this.validatePassword();
      else if (field === 'confirmPassword') this.validateConfirmPassword();
      else if (field === 'agreeTerms') this.validateAgreeTerms();
    },
    validateUsername() {
      this.errors.username = '';
      const value = this.formData.username;
      
      if (!value) {
        this.errors.username = '请输入用户名';
        return false;
      }
      
      if (value.length < 3) {
        this.errors.username = '用户名至少需要3个字符';
        return false;
      }
      
      if (value.length > 20) {
        this.errors.username = '用户名不能超过20个字符';
        return false;
      }
      
      if (!/^[a-zA-Z0-9_\u4e00-\u9fa5]+$/.test(value)) {
        this.errors.username = '用户名只能包含字母、数字、下划线和中文';
        return false;
      }
      
      return true;
    },
    validateEmail() {
      this.errors.email = '';
      const value = this.formData.email;
      
      if (!value) {
        this.errors.email = '请输入电子邮箱';
        return false;
      }
      
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(value)) {
        this.errors.email = '请输入有效的电子邮箱地址';
        return false;
      }
      
      return true;
    },
    validatePhone() {
      this.errors.phone = '';
      const value = this.formData.phone;
      
      if (!value) {
        this.errors.phone = '请输入手机号码';
        return false;
      }
      
      if (!/^1[3-9]\d{9}$/.test(value)) {
        this.errors.phone = '请输入有效的手机号码';
        return false;
      }
      
      return true;
    },
    validatePassword() {
      this.errors.password = '';
      const value = this.formData.password;
      
      if (!value) {
        this.errors.password = '请输入密码';
        return false;
      }
      
      if (value.length < 6) {
        this.errors.password = '密码至少需要6个字符';
        return false;
      }
      
      // 如果确认密码已经填写，则检查两者是否匹配
      if (this.formData.confirmPassword) {
        this.validateConfirmPassword();
      }
      
      return true;
    },
    validateConfirmPassword() {
      this.errors.confirmPassword = '';
      const value = this.formData.confirmPassword;
      
      if (!value) {
        this.errors.confirmPassword = '请确认密码';
        return false;
      }
      
      if (value !== this.formData.password) {
        this.errors.confirmPassword = '两次输入的密码不匹配';
        return false;
      }
      
      return true;
    },
    validateAgreeTerms() {
      this.errors.agreeTerms = '';
      
      if (!this.formData.agreeTerms) {
        this.errors.agreeTerms = '请同意服务条款和隐私政策';
        return false;
      }
      
      return true;
    },
    validateAll() {
      const usernameValid = this.validateUsername();
      const emailValid = this.validateEmail();
      const phoneValid = this.validatePhone();
      const passwordValid = this.validatePassword();
      const confirmPasswordValid = this.validateConfirmPassword();
      const agreeTermsValid = this.validateAgreeTerms();
      
      return usernameValid && emailValid && phoneValid && passwordValid && confirmPasswordValid && agreeTermsValid;
    },
    handleRegister() {
      // 防止重复提交
      if (this.isLoading) return;
      
      // 清空全局错误消息
      this.errorMessage = '';
      this.successMessage = '';
      
      // 验证所有字段
      if (!this.validateAll()) {
        return;
      }
      
      this.isLoading = true;
      
      // 准备提交的数据
      const userData = {
        username: this.formData.username,
        email: this.formData.email,
        phone: this.formData.phone,
        password: this.formData.password
      };
      
      // 发送注册请求
      axios.post('/api/user/register', userData)
        .then(response => {
          this.isLoading = false;
          
          if (response.data.code === 200) {
            // 注册成功
            this.successMessage = '账号注册成功！即将跳转到登录页面...';
            
            // 清空表单
            this.formData = {
              username: '',
              email: '',
              phone: '',
              password: '',
              confirmPassword: '',
              agreeTerms: false
            };
            
            // 3秒后跳转到登录页面
            setTimeout(() => {
              this.$router.push('/login');
            }, 3000);
          } else {
            // 注册失败
            this.errorMessage = response.data.msg || '注册失败，请稍后再试';
          }
        })
        .catch(error => {
          this.isLoading = false;
          
          // 详细记录错误信息，帮助调试
          console.error('注册请求错误详情:', {
            message: error.message,
            response: error.response ? {
              status: error.response.status,
              statusText: error.response.statusText,
              data: error.response.data
            } : 'No response',
            request: error.request ? 'Request sent but no response' : 'Request not sent',
            config: error.config
          });
          
          if (error.response) {
            // 服务器返回了错误状态码
            this.errorMessage = `服务器响应错误 (${error.response.status}): ${error.response.data.msg || error.response.statusText}`;
          } else if (error.request) {
            // 请求已发送但没有收到响应
            this.errorMessage = '服务器无响应，请检查网络连接或服务器状态';
          } else {
            // 请求配置有误
            this.errorMessage = `请求错误: ${error.message}`;
          }
        });
    },
    addGlassMorphismEffect() {
      // 为形状添加动态效果
      const shapes = document.querySelectorAll('.background-shapes .shape');
      shapes.forEach(shape => {
        const randomX = (Math.random() - 0.5) * 20;
        const randomY = (Math.random() - 0.5) * 20;
        shape.style.transform = `translate(${randomX}px, ${randomY}px)`;
      });
    },
    handleMouseMove(e) {
      // 光标跟随效果
      const shapes = document.querySelectorAll('.background-shapes .shape');
      const { clientX, clientY } = e;
      const centerX = window.innerWidth / 2;
      const centerY = window.innerHeight / 2;
      
      shapes.forEach((shape, index) => {
        const factor = (index + 1) * 0.01;
        const offsetX = (clientX - centerX) * factor;
        const offsetY = (clientY - centerY) * factor;
        
        shape.style.transform = `translate(${offsetX}px, ${offsetY}px)`;
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

.register-container {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  background: linear-gradient(135deg, #8e44ad 0%, #3498db 100%);
  position: relative;
  overflow: hidden;
}

.background-shapes .shape {
  position: absolute;
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
  border-radius: 50%;
}

.shape-1 {
  width: 300px;
  height: 300px;
  top: -150px;
  left: -150px;
  background: linear-gradient(135deg, rgba(142, 68, 173, 0.2), rgba(41, 128, 185, 0.2));
}

.shape-2 {
  width: 200px;
  height: 200px;
  bottom: -100px;
  right: -100px;
  background: linear-gradient(135deg, rgba(41, 128, 185, 0.2), rgba(142, 68, 173, 0.2));
}

.shape-3 {
  width: 150px;
  height: 150px;
  bottom: 50%;
  left: 10%;
  background: rgba(255, 255, 255, 0.1);
}

.shape-4 {
  width: 100px;
  height: 100px;
  top: 30%;
  right: 10%;
  background: rgba(255, 255, 255, 0.1);
}

.register-content {
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

.register-left {
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

.register-right {
  width: 50%;
  padding: 60px;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  background: rgba(255, 255, 255, 0.95);
  overflow-y: auto;
}

.register-form-container {
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

.register-form {
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

.form-group.error input {
  border-color: #d32f2f;
  background: #fff9f9;
}

.form-group.error .input-icon {
  color: #d32f2f;
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

.error-message.terms-error {
  margin-top: 0;
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

.password-strength {
  margin-top: 10px;
  font-size: 14px;
}

.strength-text {
  display: flex;
  align-items: center;
  gap: 5px;
  margin-bottom: 5px;
  color: #666;
}

.strength-text .very-weak { color: #ff4d4d; }
.strength-text .weak { color: #ff9933; }
.strength-text .medium { color: #ffcc00; }
.strength-text .strong { color: #66cc33; }
.strength-text .very-strong { color: #33cc66; }

.strength-bar {
  height: 4px;
  background: #eee;
  border-radius: 2px;
  overflow: hidden;
}

.strength-indicator {
  height: 100%;
  transition: width 0.3s ease, background-color 0.3s ease;
}

.form-options {
  display: flex;
  margin: 5px 0;
}

.accept-terms {
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

.terms-link {
  color: #8e44ad;
  text-decoration: none;
  transition: all 0.2s ease;
}

.terms-link:hover {
  text-decoration: underline;
  color: #9b59b6;
}

.register-button {
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

.register-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 7px 14px rgba(0, 0, 0, 0.1);
}

.register-button:disabled {
  background: #ccc;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.success-message {
  display: flex;
  align-items: center;
  padding: 10px 15px;
  background: #e8f5e9;
  color: #2e7d32;
  border-radius: 8px;
  margin-top: 5px;
  font-size: 14px;
  border-left: 3px solid #2e7d32;
}

.success-message .material-icon {
  margin-right: 10px;
  font-size: 20px;
}

.global-error {
  margin-top: 10px;
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

.register-footer {
  margin-top: 30px;
  text-align: center;
  color: #666;
  font-size: 0.95rem;
}

.register-footer a {
  color: #8e44ad;
  text-decoration: none;
  font-weight: 500;
}

.register-footer a:hover {
  text-decoration: underline;
  color: #9b59b6;
}

.login-link {
  color: #8e44ad;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
}

.login-link:hover {
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
  .register-content {
    max-width: 1000px;
  }
  
  .register-left {
    padding: 40px;
  }
  
  .feature-text {
    font-size: 1rem;
  }
}

@media (max-width: 992px) {
  .register-content {
    flex-direction: column;
    height: auto;
    min-height: auto;
    max-width: 600px;
  }
  
  .register-left {
    width: 100%;
    padding: 40px;
    clip-path: none;
  }
  
  .register-right {
    width: 100%;
    padding: 40px;
    max-height: 600px;
  }
  
  .register-form-container {
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
  .register-container {
    padding: 15px;
  }
  
  .register-left, .register-right {
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